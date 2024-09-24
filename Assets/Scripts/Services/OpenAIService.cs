using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;

public class OpenAIService : MonoBehaviour
{
    public static OpenAIService Instance { get; private set; }

    public enum OpenAIModel
    {
        GPT4o,
        GPT4oMini,
        FineTunedNaturalDialog
    }

    [SerializeField] private string apiKey;
    [SerializeField] private OpenAIModel selectedModel = OpenAIModel.GPT4o;
    [SerializeField] private float apiCallCooldown = 1f;

    private readonly HttpClient httpClient = new HttpClient();
    private float lastApiCallTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private string ModelToString(OpenAIModel model)
    {
        switch (model)
        {
            case OpenAIModel.GPT4o:
                return "gpt-4o";
            case OpenAIModel.GPT4oMini:
                return "gpt-4o-mini";
            case OpenAIModel.FineTunedNaturalDialog:
                return "ft:gpt-4o-2024-08-06:karyo-studios:naturaldialog3:A7A1XgRr";
            default:
                return "gpt-4o";
        }
    }

    // New Method: Generate Greeting Response
    public async Task<string> GenerateGreetingResponse(string characterName, AISettings aiSettings)
    {
        await EnforceApiCooldown();

        string prompt = GenerateGreetingPrompt(characterName, aiSettings);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

        // Ensure the greeting is within 10 words
        if (!string.IsNullOrEmpty(response))
        {
            string[] words = response.Split(' ');
            if (words.Length > 10)
            {
                response = string.Join(" ", words.Take(10));
            }
        }

        return string.IsNullOrEmpty(response) ? "Hello!" : response.Trim();
    }

    // Existing Method: Get Generative Choices
    public async Task<List<DialogueOption>> GetGenerativeChoices(string characterName, string context, AISettings aiSettings)
    {
        await EnforceApiCooldown();

        string prompt = GenerateGenerativeChoicesPrompt(characterName, context, aiSettings);
        string response = await GetChatCompletionAsync(prompt, selectedModel);

        if (string.IsNullOrEmpty(response))
        {
            return GetDefaultDialogueOptions();
        }

        List<DialogueOption> choices = ParseDialogueOptions(response);
        lastApiCallTime = Time.time;

        return choices;
    }

    // Existing Method: Get Natural Dialogue Response
    public async Task<string> GetNaturalDialogueResponse(string characterName, string playerInput, AISettings aiSettings)
    {
        await EnforceApiCooldown();

        string prompt = GenerateNaturalDialoguePrompt(characterName, playerInput, aiSettings);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Hmm, I need to think about that..." : response.Trim();
    }

    // Existing Method: Get Response (used for generalized responses)
    public async Task<string> GetResponse(string prompt, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        await EnforceApiCooldown();

        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";

        string fullPrompt = aiSettings != null
            ? $"You are a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n" +
              $"Recent memories: {memoryContext}\n\n" +
              $"Your current reflection: {reflection}\n\n" +
              $"{eurekaContext}\n\n{prompt}\n\n" +
              "Respond in character, keeping your response concise (max 50 words) and natural. Consider your memories and current reflection in your response:"
            : prompt;

        string response = await GetChatCompletionAsync(fullPrompt, selectedModel);
        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Not sure how to respond to that..." : response;
    }

    // Existing Method: Generate Eureka Description
    public async Task<string> GenerateEurekaDescription(List<UniversalCharacterController> collaborators, GameState gameState, string actionName)
    {
        string collaboratorNamesAndRoles = string.Join(", ", collaborators.Select(c => $"{c.characterName} ({c.aiSettings.characterRole})"));
        string collaboratorBackgrounds = string.Join("; ", collaborators.Select(c => $"{c.characterName}'s background: {c.aiSettings.characterBackground}"));
        string collaboratorPersonalities = string.Join("; ", collaborators.Select(c => $"{c.characterName}'s personality: {c.aiSettings.characterPersonality}"));
        string collaboratorGoals = string.Join("; ", collaborators.Select(c => $"{c.characterName}'s goals: {string.Join(", ", c.aiSettings.personalGoalTags)}"));

        string recentEurekas = string.Join("; ", EurekaManager.Instance.GetRecentEurekas());

        string prompt = $@"{collaboratorNamesAndRoles} collaborated on the action '{actionName}' as part of the challenge '{gameState.CurrentChallenge.title}'.
{collaboratorBackgrounds}
{collaboratorPersonalities}
{collaboratorGoals}
Current game progress: {gameState.CollectiveProgress}% complete.
Completed milestones: {string.Join(", ", gameState.MilestoneCompletion.Where(m => m.Value).Select(m => m.Key))}
Incomplete milestones: {string.Join(", ", gameState.MilestoneCompletion.Where(m => !m.Value).Select(m => m.Key))}
Recent Eureka moments: {recentEurekas}
Describe an unexpected and significant breakthrough resulting from their collaboration. Emphasize the interplay of their diverse roles, backgrounds, and personalities, and how this led to solving a key aspect of the challenge. Be concise (max 30 words) and compelling:";

        return await GetResponse(prompt, null);
    }

    // Existing Method: Generate Scenarios
    public async Task<List<EmergentScenarioGenerator.ScenarioData>> GenerateScenarios(GameState gameState, List<string> recentPlayerActions)
    {
        await EnforceApiCooldown();

        string completedMilestones = string.Join(", ", gameState.MilestoneCompletion.Where(m => m.Value).Select(m => m.Key));
        string incompleteMilestones = string.Join(", ", gameState.MilestoneCompletion.Where(m => !m.Value).Select(m => m.Key));
        string topPlayers = string.Join(", ", gameState.PlayerScores.OrderByDescending(kv => kv.Value).Take(3).Select(kv => $"{kv.Key} ({kv.Value} points)"));

        string prompt = $@"Current challenge: '{gameState.CurrentChallenge.title}'
Completed milestones: {completedMilestones}
Incomplete milestones: {incompleteMilestones}
Collective progress: {gameState.CollectiveProgress}%
Top players: {topPlayers}
Time remaining: {Mathf.FloorToInt(gameState.RemainingTime / 60)} minutes
Recent player actions: {string.Join(", ", recentPlayerActions)}

Based on this game state, generate three distinct, high-stakes 'What If...?' scenarios that could dramatically alter the course of the challenge. Each scenario should:
1. Start with '...'
2. Present a unique, unexpected development or complication
3. Relate to the current challenge and game state
4. Have potential for both positive and negative outcomes
5. Encourage strategic thinking and collaboration
6. Be concise but impactful (max 20 words)

Format the response as follows:
... [Scenario 1]
... [Scenario 2]
... [Scenario 3]";

        string response = await GetChatCompletionAsync(prompt, selectedModel);
        lastApiCallTime = Time.time;

        return ParseScenarioResponse(response);
    }

    private List<EmergentScenarioGenerator.ScenarioData> ParseScenarioResponse(string response)
    {
        var scenarios = new List<EmergentScenarioGenerator.ScenarioData>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("..."))
            {
                scenarios.Add(new EmergentScenarioGenerator.ScenarioData
                {
                    description = line.Trim()
                });
            }
        }

        return scenarios;
    }

    // New Method: Generate Greeting Prompt
    private string GenerateGreetingPrompt(string characterName, AISettings aiSettings)
    {
        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n" +
               "Generate a friendly and concise greeting (max 10 words) that you would say when initiating a conversation with a player. " +
               "The greeting should feel natural and may reflect your personality traits.";
    }

    private string GenerateGenerativeChoicesPrompt(string characterName, string context, AISettings aiSettings)
    {
        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n" +
            $"Based on this context: {context}\n\n" +
            "Generate 3 short, distinct responses (max 8 words each) that {characterName} might consider. " +
            "These can be a mix of casual conversational responses and action choices. " +
            "For action choices, use one of these categories: Ethical, Strategic, Emotional, Practical, Creative, Diplomatic, or Risk-Taking. " +
            "For casual responses, use the Casual category. " +
            "Format your response as follows:\n" +
            "1. [Category]: [Response]\n" +
            "2. [Category]: [Response]\n" +
            "3. [Category]: [Response]";
    }

    private string GenerateNaturalDialoguePrompt(string characterName, string playerInput, AISettings aiSettings)
    {
        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n" +
               $"The player says: \"{playerInput}\"\n" +
               "Respond naturally and in character.";
    }

    private List<DialogueOption> GetDefaultDialogueOptions()
    {
        return new List<DialogueOption>
        {
            new DialogueOption("Investigate the area", DialogueCategory.Practical),
            new DialogueOption("Collaborate with a nearby character", DialogueCategory.Diplomatic),
            new DialogueOption("Propose an innovative solution", DialogueCategory.Creative)
        };
    }

    private List<DialogueOption> ParseDialogueOptions(string response)
    {
        List<DialogueOption> options = new List<DialogueOption>();
        string[] lines = response.Split('\n');

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                string categoryStr = parts[0].Trim().Replace("1. ", "").Replace("2. ", "").Replace("3. ", "");
                string choiceText = parts[1].Trim();

                if (categoryStr.Equals("Casual", StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new DialogueOption(choiceText, DialogueCategory.Casual));
                }
                else if (Enum.TryParse(categoryStr, out DialogueCategory category))
                {
                    options.Add(new DialogueOption(choiceText, category));
                }
            }
        }

        return options;
    }

    // Existing Method: Get Chat Completion from OpenAI
    private async Task<string> GetChatCompletionAsync(string prompt, OpenAIModel model)
    {
        var requestBody = new
        {
            model = ModelToString(model),
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 150
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"OpenAI API request failed: {responseString}");
                return null;
            }

            var responseJson = JObject.Parse(responseString);
            return responseJson["choices"][0]["message"]["content"].ToString().Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OpenAI API request: {e.Message}");
            return null;
        }
    }

    // New Method: Enforce API Call Cooldown
    private async Task EnforceApiCooldown()
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            float waitTime = apiCallCooldown - (Time.time - lastApiCallTime);
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
        }
    }
}