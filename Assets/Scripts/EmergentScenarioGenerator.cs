using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Photon.Pun;

public class EmergentScenarioGenerator : MonoBehaviour
{
    private OpenAIService openAIService;

    private void Start()
    {
        openAIService = OpenAIService.Instance;
    }

    public async Task<string> GenerateScenario(string currentChallenge, List<string> recentPlayerActions)
    {
        string prompt = $"Based on the current challenge '{currentChallenge}' and recent player actions: {string.Join(", ", recentPlayerActions)}, generate a new emergent scenario that adds complexity to the game. The scenario should be a single paragraph describing an unexpected event or complication.";

        string response = await openAIService.GetChatCompletionAsync(prompt);
        return response;
    }

    public void ApplyScenario(string scenario)
    {
        // TODO: Implement logic to apply the generated scenario to the game state
        Debug.Log($"New scenario applied: {scenario}");
        
        // Notify all players of the new scenario
        GameplayManager.Instance.photonView.RPC("RPC_NotifyNewScenario", RpcTarget.All, scenario);
    }
}