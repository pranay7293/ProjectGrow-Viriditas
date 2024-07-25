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

    [SerializeField] private string apiKey;
    [SerializeField] private string model = "gpt-4";
    [SerializeField] private float apiCallCooldown = 1f; // Cooldown in seconds

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

    public async Task<List<string>> GetGenerativeChoices(string characterName, string context)
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            await Task.Delay(TimeSpan.FromSeconds(apiCallCooldown - (Time.time - lastApiCallTime)));
        }

        string prompt = $"Generate 3 short, distinct action choices (max 10 words each) for {characterName} based on this context: {context}";
        string response = await GetChatCompletionAsync(prompt);

        if (string.IsNullOrEmpty(response))
        {
            return new List<string> { "Investigate the area", "Talk to a nearby character", "Work on the current objective" };
        }

        List<string> choices = new List<string>(response.Split('\n'));
        lastApiCallTime = Time.time;

        return choices;
    }

    public async Task<string> GetResponse(string prompt)
    {
        if (Time.time - lastApiCallTime < apiCallCooldown)
        {
            await Task.Delay(TimeSpan.FromSeconds(apiCallCooldown - (Time.time - lastApiCallTime)));
        }

        string response = await GetChatCompletionAsync(prompt);
        lastApiCallTime = Time.time;

        return string.IsNullOrEmpty(response) ? "I'm not sure how to respond to that." : response;
    }

    private async Task<string> GetChatCompletionAsync(string prompt)
    {
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 100
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