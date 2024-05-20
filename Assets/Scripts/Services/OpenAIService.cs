using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Threading.Tasks;

public class OpenAIService : MonoBehaviour
{
    [SerializeField]
    [InfoBox("Do not check in API key to source.", InfoMessageType.Warning)]
    private string apiKey;

    private OpenAIClient client;

    private void Awake()
    {
        var authentication = new OpenAIAuthentication(apiKey);
        var settings = new OpenAISettings();
        client = new OpenAIClient(authentication, settings);
    }

    public async Task<string> GetChatCompletionAsync(Model model, string systemPrompt, string userInput)
    {
        var messages = new List<Message> {
            new Message(Role.System, systemPrompt),
            new Message(Role.User, userInput),
        };
        var request = new ChatRequest(messages, model);
        var response = await client.ChatEndpoint.GetCompletionAsync(request);
        return response.FirstChoice.Message;
    }
}
