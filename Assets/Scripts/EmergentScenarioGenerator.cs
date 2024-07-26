using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Photon.Pun;

public class EmergentScenarioGenerator : MonoBehaviourPunCallbacks
{
    private OpenAIService openAIService;
    [SerializeField] private AISettings scenarioGeneratorSettings;

    private void Start()
    {
        openAIService = OpenAIService.Instance;
        if (openAIService == null)
        {
            Debug.LogError("OpenAIService not found in the scene. Please add it to continue.");
        }
        
        if (scenarioGeneratorSettings == null)
        {
            Debug.LogError("Scenario Generator AI Settings not assigned. Please assign it in the inspector.");
        }
    }

    public async Task<string> GenerateScenario(string currentChallenge, List<string> recentPlayerActions)
    {
        string prompt = $"Based on the current challenge '{currentChallenge}' and recent player actions: {string.Join(", ", recentPlayerActions)}, create a brief emergent scenario. The scenario should be a single paragraph describing an unexpected event or complication.";

        return await openAIService.GetResponse(prompt, scenarioGeneratorSettings);
    }

    public void ApplyScenario(string scenario)
    {
        photonView.RPC("RPC_NotifyNewScenario", RpcTarget.All, scenario);
    }

    [PunRPC]
    private void RPC_NotifyNewScenario(string scenario)
    {
        Debug.Log($"New scenario: {scenario}");
        // TODO: Implement logic to apply the scenario to the game state
        // For example:
        // - Update UI to show the new scenario
        // - Modify game parameters based on the scenario
        // - Trigger specific events or challenges
    }
}