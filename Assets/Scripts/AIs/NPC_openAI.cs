using UnityEngine;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;

public class NPC_openAI : MonoBehaviour
{
    private NPC_Data npcData;
    private OpenAIService openAIService;

    public void Initialize(NPC_Data data)
    {
        npcData = data;
        openAIService = OpenAIService.Instance;
    }

    public async Task<List<string>> GetGenerativeChoices()
    {
        string characterContext = GetCharacterContext();
        string prompt = $"{characterContext}\n\nGenerate 3 short, distinct action choices (max 10 words each) for this character based on their personality and the current game situation. Separate the choices with a newline character.";
        
        string response = await openAIService.GetChatCompletionAsync(prompt);
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("Failed to get response from OpenAI API, using default choices");
            return new List<string> { "Investigate the area", "Talk to a nearby character", "Work on the current objective" };
        }
        return new List<string>(response.Split('\n'));
    }

    public async Task<string> GetResponse(string prompt)
    {
        string characterContext = GetCharacterContext();
        string fullPrompt = $"{characterContext}\n\n{prompt}";

        string response = await openAIService.GetChatCompletionAsync(fullPrompt);
        return string.IsNullOrEmpty(response) ? "I'm not sure how to respond to that." : response;
    }

    private string GetCharacterContext()
    {
        StringBuilder context = new StringBuilder();
        context.AppendLine($"Character: {npcData.GetCharacterName()}");
        context.AppendLine($"Role: {npcData.GetCharacterRole()}");
        context.AppendLine($"Background: {npcData.GetCharacterBackground()}");
        context.AppendLine($"Personality: {npcData.GetCharacterPersonality()}");
        context.AppendLine("Recent memories:");
        foreach (string memory in npcData.GetMemories())
        {
            context.AppendLine($"- {memory}");
        }
        context.AppendLine($"Current challenge: {GameManager.Instance.GetCurrentChallenge()}");

        return context.ToString();
    }
}