using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Photon.Pun;

public class EmergentScenarioGenerator : MonoBehaviourPunCallbacks
{
    public static EmergentScenarioGenerator Instance { get; private set; }

    [System.Serializable]
    public class ScenarioData
    {
        public string description;
        public List<string> options;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task<ScenarioData> GenerateScenario(string currentChallenge, List<string> recentPlayerActions)
    {
    if (OpenAIService.Instance == null)
    {
        Debug.LogError("OpenAIService not found in the scene. Please add it to continue.");
        return null;
    }

    return await OpenAIService.Instance.GenerateScenario(currentChallenge, recentPlayerActions);
    }

    public void ResolveScenario(string chosenOption)
    {
        int points = EvaluateScenarioOutcome(chosenOption);
        GameManager.Instance.UpdateCollectiveScore(points);
        GameManager.Instance.UpdateGameState("System", chosenOption);
        GameManager.Instance.ResetPlayerPositions();
    }

    private int EvaluateScenarioOutcome(string chosenOption)
    {
        // Implement more sophisticated logic to determine points based on the chosen option
        return Random.Range(50, 151);
    }
}