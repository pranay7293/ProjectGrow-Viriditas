using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Photon.Pun;

public class EmergentScenarioGenerator : MonoBehaviourPunCallbacks
{
    private OpenAIService openAIService;

    private void Start()
    {
        openAIService = OpenAIService.Instance;
        if (openAIService == null)
        {
            Debug.LogError("OpenAIService not found in the scene. Please add it to continue.");
        }
    }

    public async Task<string> GenerateScenario(string currentChallenge, List<string> recentPlayerActions)
    {
        string prompt = $"Based on the current challenge '{currentChallenge}' and recent player actions: {string.Join(", ", recentPlayerActions)}, create a brief emergent scenario. The scenario should be a single paragraph describing an unexpected event or complication that affects the challenge progress.";

        return await openAIService.GetResponse(prompt, null);
    }

    public void ApplyScenario(string scenario)
    {
        photonView.RPC("RPC_NotifyNewScenario", RpcTarget.All, scenario);
    }

    [PunRPC]
    private void RPC_NotifyNewScenario(string scenario)
    {
    Debug.Log($"New scenario: {scenario}");
    GameManager.Instance.UpdateEmergentScenario(scenario);
    }

    public void ResolveScenario(string chosenOption)
    {
    int points = EvaluateScenarioOutcome(chosenOption);
    GameManager.Instance.UpdateCollectiveScore(points);
    GameManager.Instance.UpdateGameState("System", chosenOption);
    }

    private int EvaluateScenarioOutcome(string chosenOption)
    {
        // Implement more sophisticated logic to determine points based on the chosen option
        // For now, we'll use a simple random range
        return Random.Range(50, 151);
    }
}