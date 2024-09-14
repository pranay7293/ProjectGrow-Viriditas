using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class EmergentScenarioGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ScenarioData
    {
        public string description;
    }

    public async Task<List<ScenarioData>> GenerateScenarios(GameState gameState, List<string> recentPlayerActions)
    {
        return await OpenAIService.Instance.GenerateScenarios(gameState, recentPlayerActions);
    }
}