using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

public class OpenAIService : MonoBehaviour
{
    public static OpenAIService Instance { get; private set; }

    [SerializeField]
    private string apiKey;

    private readonly HttpClient httpClient = new HttpClient();

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

    public async Task<string> GetChatCompletionAsync(string prompt)
    {
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var content = new StringContent(JsonUtility.ToJson(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var responseString = await response.Content.ReadAsStringAsync();

        var responseJson = JObject.Parse(responseString);
        return responseJson["choices"][0]["message"]["content"].ToString();
    }
}