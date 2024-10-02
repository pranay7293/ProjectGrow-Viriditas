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
                return "gpt-4";
            case OpenAIModel.GPT4oMini:
                return "gpt-3.5-turbo";
            case OpenAIModel.FineTunedNaturalDialog:
                return "ft:gpt-4o-2024-08-06:karyo-studios:naturaldialog3:A7A1XgRr";
            default:
                return "gpt-4";
        }
    }

     // Method: Generate Agent Greeting
    public async Task<string> GenerateAgentGreeting(string characterName, AISettings aiSettings)
    {
        await EnforceApiCooldown();

        string prompt = GenerateAgentGreetingPrompt(characterName, aiSettings);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

        // Ensure the greeting is within 15 words
        if (!string.IsNullOrEmpty(response))
        {
            string[] words = response.Split(' ');
            if (words.Length > 15)
            {
                response = string.Join(" ", words.Take(15));
            }
        }

        return string.IsNullOrEmpty(response) ? "Hello!" : response.Trim();
    }

    // Method: Get Generative Choices
    public async Task<List<GenerativeChoiceOption>> GetGenerativeChoices(string characterName, string context, AISettings aiSettings)
    {
        await EnforceApiCooldown();

        string prompt = GenerateGenerativeChoicesPrompt(characterName, context, aiSettings);
        string response = await GetChatCompletionAsync(prompt, selectedModel);

        if (string.IsNullOrEmpty(response))
        {
            return GetDefaultGenerativeChoices();
        }

        List<GenerativeChoiceOption> choices = ParseGenerativeChoices(response);
        lastApiCallTime = Time.time;

        return choices;
    }

    // Method: Get Agent Response to Player Input
    public async Task<string> GetAgentResponse(string characterName, string playerInput, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        await EnforceApiCooldown();

        string prompt = GenerateAgentResponsePrompt(characterName, playerInput, aiSettings, memoryContext, reflection);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Hmm, I need to think about that..." : response.Trim();
    }

    // Method: Get Agent Response to Generative Choice
    public async Task<string> GetAgentResponseToChoice(string characterName, string playerChoice, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        await EnforceApiCooldown();

        string prompt = GenerateAgentResponseToChoicePrompt(characterName, playerChoice, aiSettings, memoryContext, reflection);
        string response = await GetChatCompletionAsync(prompt, selectedModel);

        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Let me consider that option..." : response.Trim();
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

    // Generate Agent Greeting Prompt
    private string GenerateAgentGreetingPrompt(string characterName, AISettings aiSettings)
    {
        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} " +
               $"Your personality: {aiSettings.characterPersonality}\n\n" +
               "Initiate a conversation with a natural and friendly greeting that reflects your personality. " +
               "Keep it concise (max 15 words).";
    }

    // Generate Generative Choices Prompt
    private string GenerateGenerativeChoicesPrompt(string characterName, string context, AISettings aiSettings)
    {
        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} " +
               $"Your personality: {aiSettings.characterPersonality}\n\n" +
               $"Based on this context: {context}\n\n" +
               $"Generate 3 high-stakes decisions that {characterName} might propose to the player. " +
               $"Each decision should be impactful and fall into one of these categories: Ethical, Strategic, Emotional, Practical, Creative, Diplomatic, or RiskTaking.\n" +
               $"Each decision should be concise (max 12 words) and clearly worded.\n" +
               "Format your response as follows:\n" +
               "1. [Category]: [Decision]\n" +
               "2. [Category]: [Decision]\n" +
               "3. [Category]: [Decision]";
    }

    // Generate Agent Response Prompt
    private string GenerateAgentResponsePrompt(string characterName, string playerInput, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";

        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} " +
               $"Your personality: {aiSettings.characterPersonality}\n" +
               $"Recent memories: {memoryContext}\n" +
               $"Your current reflection: {reflection}\n" +
               $"{eurekaContext}\n\n" +
               $"The player says: \"{playerInput}\"\n" +
               "Respond naturally and in character.";
    }

    // Generate Agent Response to Generative Choice Prompt
    private string GenerateAgentResponseToChoicePrompt(string characterName, string playerChoice, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";

        return $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} " +
               $"Your personality: {aiSettings.characterPersonality}\n" +
               $"Recent memories: {memoryContext}\n" +
               $"Your current reflection: {reflection}\n" +
               $"{eurekaContext}\n\n" +
               $"The player has chosen: \"{playerChoice}\"\n" +
               "Provide a response that reflects your thoughts on this choice, keeping in character and being concise (max 50 words).";
    }

    private List<GenerativeChoiceOption> GetDefaultGenerativeChoices()
    {
        return new List<GenerativeChoiceOption>
        {
            new GenerativeChoiceOption("Investigate the anomaly", GenerativeChoiceCategory.Practical),
            new GenerativeChoiceOption("Collaborate with a nearby character", GenerativeChoiceCategory.Diplomatic),
            new GenerativeChoiceOption("Take a calculated risk", GenerativeChoiceCategory.RiskTaking)
        };
    }

    private List<GenerativeChoiceOption> ParseGenerativeChoices(string response)
    {
        List<GenerativeChoiceOption> options = new List<GenerativeChoiceOption>();
        string[] lines = response.Split('\n');

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                string categoryStr = parts[0].Trim().Replace("1. ", "").Replace("2. ", "").Replace("3. ", "");
                string choiceText = parts[1].Trim();

                if (Enum.TryParse(categoryStr, true, out GenerativeChoiceCategory category))
                {
                    options.Add(new GenerativeChoiceOption(choiceText, category));
                }
                else
                {
                    Debug.LogWarning($"Unknown category '{categoryStr}' in generative choice.");
                }
            }
        }

        return options;
    }

    // Existing Method: Generate Eureka Description
    public async Task<string> GenerateEurekaDescription(List<UniversalCharacterController> collaborators, GameState gameState, string actionName)
    {
        await EnforceApiCooldown();

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
6. Be concise but impactful (max 15 words)

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

    // Method to get chat completion from OpenAI
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

    // Enforce API call cooldown
    private async Task EnforceApiCooldown()
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            float waitTime = apiCallCooldown - (Time.time - lastApiCallTime);
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
        }
    }
}