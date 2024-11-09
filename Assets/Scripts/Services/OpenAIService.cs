using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

public class OpenAIService : MonoBehaviour
{
    [SerializeField] private APIKeyConfig apiKeyConfig;
    public static OpenAIService Instance { get; private set; }

    public enum OpenAIModel
    {
        GPT4o,
        GPT4oMini,
        FineTunedNaturalDialog,
        O1Preview
    }

    [SerializeField] private OpenAIModel selectedModel = OpenAIModel.GPT4o;
    [SerializeField] private float apiCallCooldown = 1f;

    private readonly HttpClient httpClient = new HttpClient();
    private float lastApiCallTime;
    private string apiKey;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEnvironmentVariables();
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("OpenAI API key not found in environment variables!");
                return;
            }
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadEnvironmentVariables()
    {
#if UNITY_EDITOR
        // In editor, still try to load from .env first
        try
        {
            string envPath = Path.Combine(Application.dataPath, "../.env");
            if (File.Exists(envPath))
            {
                foreach (string line in File.ReadAllLines(envPath))
                {
                    if (line.StartsWith("OPENAI_API_KEY="))
                    {
                        apiKey = line.Substring("OPENAI_API_KEY=".Length).Trim();
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading from .env: {e.Message}");
        }
#endif

        // Fall back to ScriptableObject config
        if (apiKeyConfig != null)
        {
            apiKey = apiKeyConfig.OpenAIKey;
        }
        else
        {
            Debug.LogError("API Key Config not assigned!");
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
            case OpenAIModel.O1Preview:
                return "o1-preview";
            default:
                return "gpt-4o";
        }
    }

    public async Task<string> GenerateAgentGreeting(string characterName, AISettings aiSettings, string interactionHistory)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return "Hello!";
        }

        await EnforceApiCooldown();

        string prompt = GenerateAgentGreetingPrompt(characterName, aiSettings, interactionHistory);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

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

    public async Task<List<GenerativeChoiceOption>> GetGenerativeChoices(string characterName, string context, AISettings aiSettings)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return GetDefaultGenerativeChoices();
        }

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

    public async Task<string> GetAgentResponse(string characterName, string playerInput, AISettings aiSettings, string interactionHistory, string memoryContext = "", string reflection = "")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return "Hmm, I need to think about that...";
        }

        await EnforceApiCooldown();

        string prompt = GenerateAgentResponsePrompt(characterName, playerInput, aiSettings, interactionHistory, memoryContext, reflection);
        string response = await GetChatCompletionAsync(prompt, OpenAIModel.FineTunedNaturalDialog);

        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Hmm, I need to think about that..." : response.Trim();
    }

    public async Task<string> GetAgentResponseToChoice(string characterName, string playerChoice, AISettings aiSettings, string interactionHistory, string memoryContext = "", string reflection = "")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return "Let me consider that option...";
        }

        await EnforceApiCooldown();

        string prompt = GenerateAgentResponseToChoicePrompt(characterName, playerChoice, aiSettings, interactionHistory, memoryContext, reflection);
        string response = await GetChatCompletionAsync(prompt, selectedModel);

        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Let me consider that option..." : response.Trim();
    }

   private string GenerateAgentGreetingPrompt(string characterName, AISettings aiSettings, string interactionHistory)
{
    bool hasMetBefore = !string.IsNullOrEmpty(interactionHistory);
    string currentChallenge = GameManager.Instance.GetCurrentChallenge().title;
    
    return $@"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground}
Your personality: {aiSettings.characterPersonality}

Current challenge: {currentChallenge}
Your personal goals: {string.Join(", ", aiSettings.personalGoals)}
Your current work: You're working on {(hasMetBefore ? "our" : "the")} {currentChallenge} challenge.

Interaction History:
{(hasMetBefore ? interactionHistory : "This is your first time meeting this person.")}

{(hasMetBefore ? 
    "Continue the ongoing relationship by greeting them naturally, acknowledging your shared work on the challenge." : 
    "Introduce yourself, mentioning your role and current work on the challenge.")}

Guidelines:
- {(hasMetBefore ? "Reference past interactions and shared progress" : "Make a proper first introduction")}
- Stay focused on your role and the current challenge
- Be natural and professional
- Keep response concise (max 15 words)
- {(hasMetBefore ? "Maintain conversation continuity" : "Establish initial connection")}";
}

    private string GenerateGenerativeChoicesPrompt(string characterName, string context, AISettings aiSettings)
    {
        return $@"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground}
Your personality: {aiSettings.characterPersonality}

Current context:
{context}

Your personal goals: {string.Join(", ", aiSettings.personalGoals)}

Generate 3 high-stakes decisions that you might propose, considering:
1. Your expertise and role
2. The current challenge and context
3. Your personal goals and motivations
4. Recent developments and discoveries

Each decision should:
- Be impactful and meaningful
- Relate to current context
- Reflect your expertise
- Fall into one of these categories: Ethical, Strategic, Emotional, Practical, Creative, Diplomatic, or RiskTaking
- Be concise (max 12 words)

Format your response as:
1. [Category]: [Decision]
2. [Category]: [Decision]
3. [Category]: [Decision]";
    }

    private string GenerateAgentResponsePrompt(string characterName, string playerInput, AISettings aiSettings, string interactionHistory, string memoryContext = "", string reflection = "")
    {
        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";
        
        return $@"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground}
Your personality: {aiSettings.characterPersonality}

Current conversation context:
{interactionHistory}

Your relevant memories and thoughts:
{memoryContext}
Current reflection: {reflection}

Challenge context:
- Current challenge: {GameManager.Instance.GetCurrentChallenge().title}
- Your personal goals: {string.Join(", ", aiSettings.personalGoals)}
- Recent discoveries: {eurekaContext}

The other person just said: ""{playerInput}""

Respond naturally while:
1. Maintaining conversation continuity - directly reference and build upon what was just said
2. Keeping consistent with your previous statements in this conversation
3. Drawing on relevant memories and past interactions
4. Staying focused on current topics and goals
5. Expressing your character's personality and expertise

Keep your response focused and contextual. Show you're actively engaged in an ongoing conversation.";
    }

    private string GenerateAgentResponseToChoicePrompt(string characterName, string playerChoice, AISettings aiSettings, string interactionHistory, string memoryContext = "", string reflection = "")
    {
        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";

        return $@"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground}
Your personality: {aiSettings.characterPersonality}

Current conversation context:
{interactionHistory}

Your relevant memories and thoughts:
{memoryContext}
Current reflection: {reflection}

Challenge context:
- Current challenge: {GameManager.Instance.GetCurrentChallenge().title}
- Your personal goals: {string.Join(", ", aiSettings.personalGoals)}
- Recent discoveries: {eurekaContext}

The other person has chosen this action: ""{playerChoice}""

Respond while:
1. Directly addressing their specific choice
2. Maintaining conversation flow from previous exchanges
3. Drawing on relevant memories and expertise
4. Staying focused on current goals and challenge
5. Keeping your character's personality consistent

Provide a natural response that shows you're engaged in an ongoing conversation and considering their specific choice.
Keep response concise (max 25 words) but maintain conversation continuity.";
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

    public async Task<(string description, List<string> tags)> GenerateEurekaDescriptionAndTags(List<UniversalCharacterController> collaborators, GameState gameState, string actionName)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return ("Unexpected breakthrough occurred!", new List<string>());
        }

        await EnforceApiCooldown();

        string staticPrompt = @"
Generate an unexpected and exciting breakthrough or discovery resulting from character collaboration.
Focus on the unique insight or innovation that emerges from the combination of their expertise.
Be concise (max 30 words), clear, and capture the imagination. The output can be unconventional or even weird, but should feel believable within the context.

Based on the description, generate a list of 3 to 5 relevant tags that match the existing PersonalGoalTags and/or MilestoneTags.
Choose only from the following tags: ";

        List<string> validTags = new List<string>();
        validTags.AddRange(TagSystem.PersonalGoalTagsList);
        validTags.AddRange(TagSystem.MilestoneTagsList);
        staticPrompt += string.Join(", ", validTags) + "\n\n";

        string dynamicPrompt = $@"Challenge: '{gameState.CurrentChallenge.title}'
Collaborators: {string.Join(", ", collaborators.Select(c => $"{c.characterName} ({c.aiSettings.characterRole})"))}
Action: '{actionName}'
Game Progress: {gameState.CollectiveProgress}%
Completed Milestones: {string.Join(", ", gameState.MilestoneCompletion.Where(m => m.Value).Select(m => m.Key))}
Incomplete Milestones: {string.Join(", ", gameState.MilestoneCompletion.Where(m => !m.Value).Select(m => m.Key))}
Recent Eureka Moments: {string.Join("; ", EurekaManager.Instance.GetRecentEurekas())}

Provide the response in the following format:
Description: [Your generated description here]
Tags: [comma-separated list of 3-5 tags from the provided list]";

        string fullPrompt = staticPrompt + dynamicPrompt;
        string response = await GetChatCompletionAsync(fullPrompt, OpenAIModel.O1Preview);

        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("No response from OpenAI API when generating Eureka description.");
            return ("An unexpected breakthrough occurred!", new List<string>());
        }

        string description = "";
        List<string> generatedTags = new List<string>();

        string[] parts = response.Split(new[] { "Description:", "Tags:" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            description = parts[0].Trim();
            generatedTags = parts[1].Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(tag => tag.Trim())
                                    .ToList();
        }
        else
        {
            Debug.LogWarning("Eureka response not properly formatted.");
            return (response.Trim(), new List<string>());
        }

        return (description, generatedTags);
    }

    public async Task<List<EmergentScenarioGenerator.ScenarioData>> GenerateScenarios(GameState gameState, List<string> recentPlayerActions)
{
    if (string.IsNullOrEmpty(apiKey))
    {
        Debug.LogError("API key not loaded!");
        return new List<EmergentScenarioGenerator.ScenarioData>();
    }

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
    
    if (string.IsNullOrEmpty(response))
    {
        Debug.LogWarning("Empty response from API when generating scenarios");
        return scenarios;
    }

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

    public async Task<string> GetResponse(string prompt, AISettings aiSettings, string memoryContext = "", string reflection = "")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not loaded!");
            return "Not sure how to respond to that...";
        }

        await EnforceApiCooldown();

        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join("; ", recentEurekas)}" : "";

        string fullPrompt = aiSettings != null
            ? $"You are a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n" +
              $"Recent memories: {memoryContext}\n\n" +
              $"Your current reflection: {reflection}\n\n" +
              $"{eurekaContext}\n\n{prompt}\n\n" +
              "Respond in character, keeping your response concise (max 25 words) and natural. Consider your memories and current reflection in your response:"
            : prompt;

        string response = await GetChatCompletionAsync(fullPrompt, selectedModel);
        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "Not sure how to respond to that..." : response;
    }

   private async Task<string> GetChatCompletionAsync(string prompt, OpenAIModel model, int maxRetries = 3)
{
    if (string.IsNullOrEmpty(apiKey))
    {
        Debug.LogError("API key not loaded!");
        return null;
    }

    await EnforceApiCooldown();

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var requestBody = new
            {
                model = ModelToString(model),
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                // Include 'temperature' and 'top_p' only for models that support them
                temperature = (model != OpenAIModel.O1Preview && model != OpenAIModel.FineTunedNaturalDialog) ? 0.7 : (double?)null,
                top_p = (model != OpenAIModel.O1Preview && model != OpenAIModel.FineTunedNaturalDialog) ? 1.0 : (double?)null,
                max_tokens = (model != OpenAIModel.O1Preview) ? 300 : (int?)null,
                max_completion_tokens = (model == OpenAIModel.O1Preview) ? 5000 : (int?)null
            };

            // Remove null values from requestBody
            var jsonRequestBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            // Log the response for debugging
            // Debug.Log($"API Response: {responseString}");

            var responseJson = JObject.Parse(responseString);

            // Check for errors in the response
            if (responseJson["error"] != null)
            {
                string errorMessage = responseJson["error"]["message"]?.ToString();
                Debug.LogError($"OpenAI API returned an error: {errorMessage}");
                return null;
            }

            if (responseJson["choices"] == null || !responseJson["choices"].Any())
            {
                Debug.LogError("OpenAI API returned no choices in the response.");
                return null;
            }

            return responseJson["choices"][0]["message"]["content"].ToString().Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OpenAI API request (Attempt {attempt + 1}/{maxRetries}): {e.Message}");
            if (attempt == maxRetries - 1)
            {
                return null;
            }
            await Task.Delay((int)Math.Pow(2, attempt) * 1000);
        }
    }

    return null;
}

    private async Task EnforceApiCooldown()
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            float waitTime = apiCallCooldown - (Time.time - lastApiCallTime);
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
        }
        lastApiCallTime = Time.time;
    }
}