using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Photon.Pun;

public class EmergentScenarioGenerator : MonoBehaviourPunCallbacks
{
    public static EmergentScenarioGenerator Instance { get; private set; }

    private OpenAIService openAIService;

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

    private void Start()
    {
        openAIService = OpenAIService.Instance;
        if (openAIService == null)
        {
            Debug.LogError("OpenAIService not found in the scene. Please add it to continue.");
        }
    }

    public async Task<ScenarioData> GenerateScenario(string currentChallenge, List<string> recentPlayerActions)
    {
        string prompt = $"Based on the current challenge '{currentChallenge}' and recent player actions: {string.Join(", ", recentPlayerActions)}, create a brief emergent scenario. Provide a scenario description (max 3 sentences) and 3 possible response options (1 sentence each). Format the response as JSON with 'description' and 'options' fields.";

        string response = await openAIService.GetResponse(prompt, null);
        return JsonUtility.FromJson<ScenarioData>(response);
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