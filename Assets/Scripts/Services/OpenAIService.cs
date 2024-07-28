using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

public class OpenAIService : MonoBehaviour
{
    public static OpenAIService Instance { get; private set; }

    public enum OpenAIModel
    {
        GPT4o,
        GPT4oMini
    }

    [SerializeField] private string apiKey;
    [SerializeField] private OpenAIModel selectedModel = OpenAIModel.GPT4o;
    [SerializeField] private float apiCallCooldown = 1f;

    public OpenAIModel CurrentModel
    {
        get => selectedModel;
        set => selectedModel = value;
    }

    private readonly HttpClient httpClient = new HttpClient();
    private float lastApiCallTime;

    private string ModelToString(OpenAIModel model)
    {
        switch (model)
        {
            case OpenAIModel.GPT4o:
                return "gpt-4o";
            case OpenAIModel.GPT4oMini:
                return "gpt-4o-mini";
            default:
                return "gpt-4o";
        }
    }

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

    public async Task<List<DialogueOption>> GetGenerativeChoices(string characterName, string context, AISettings aiSettings)
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            await Task.Delay(TimeSpan.FromSeconds(apiCallCooldown - (Time.time - lastApiCallTime)));
        }

        string prompt = $"You are {characterName}, a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n" +
            $"Based on this context: {context}\n\n" +
            "Generate 3 short, distinct action choices (max 8 words each) that {characterName} might consider. " +
            "Each choice should fall into one of these categories: Ethical, Strategic, Emotional, Practical, Creative, Diplomatic, or Risk-Taking. " +
            "Format your response as follows:\n" +
            "1. [Category]: [Choice]\n" +
            "2. [Category]: [Choice]\n" +
            "3. [Category]: [Choice]";

        string response = await GetChatCompletionAsync(prompt);

        if (string.IsNullOrEmpty(response))
        {
            return new List<DialogueOption>
            {
                new DialogueOption("Investigate the area", DialogueCategory.Practical),
                new DialogueOption("Collaborate with a nearby character", DialogueCategory.Diplomatic),
                new DialogueOption("Propose an innovative solution", DialogueCategory.Creative)
            };
        }

        List<DialogueOption> choices = ParseDialogueOptions(response);
        lastApiCallTime = Time.time;

        return choices;
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

                if (Enum.TryParse(categoryStr, out DialogueCategory category))
                {
                    options.Add(new DialogueOption(choiceText, category));
                }
            }
        }

        return options;
    }

    public async Task<string> GetResponse(string prompt, AISettings aiSettings)
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            await Task.Delay(TimeSpan.FromSeconds(apiCallCooldown - (Time.time - lastApiCallTime)));
        }

        string fullPrompt = $"You are a {aiSettings.characterRole}. {aiSettings.characterBackground} Your personality: {aiSettings.characterPersonality}\n\n{prompt}\n\nRespond in character, keeping your response concise (max 50 words) and natural:";
        string response = await GetChatCompletionAsync(fullPrompt);
        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "I'm not sure how to respond to that." : response;
    }

    private async Task<string> GetChatCompletionAsync(string prompt)
    {
        var requestBody = new
        {
            model = ModelToString(selectedModel),
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
            return responseJson["choices"][0]["message"]["content"].ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OpenAI API request: {e.Message}");
            return null;
        }
    }
}