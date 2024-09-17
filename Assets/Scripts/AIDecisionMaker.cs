using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class AIDecisionMaker : MonoBehaviour
{
    private const int DecisionCacheSize = 10;
    private const float DecisionCacheExpirationTime = 300f; // 5 minutes

    private Dictionary<string, CachedDecision> decisionCache = new Dictionary<string, CachedDecision>();

    private class CachedDecision
    {
        public string Decision;
        public float Timestamp;
    }

    public async Task<string> MakeDecision(AIManager aiManager, List<string> options, GameState gameState, string memoryContext, string reflection)
    {
        string cacheKey = GenerateCacheKey(aiManager, options, gameState, memoryContext, reflection);

        if (decisionCache.TryGetValue(cacheKey, out CachedDecision cachedDecision))
        {
            if (Time.time - cachedDecision.Timestamp < DecisionCacheExpirationTime)
            {
                return cachedDecision.Decision;
            }
            else
            {
                decisionCache.Remove(cacheKey);
            }
        }

        string decision = await GetAIDecision(aiManager, options, gameState, memoryContext, reflection);

        decisionCache[cacheKey] = new CachedDecision { Decision = decision, Timestamp = Time.time };

        if (decisionCache.Count > DecisionCacheSize)
        {
            string oldestKey = decisionCache.OrderBy(kvp => kvp.Value.Timestamp).First().Key;
            decisionCache.Remove(oldestKey);
        }

        return decision;
    }

    private async Task<string> GetAIDecision(AIManager aiManager, List<string> options, GameState gameState, string memoryContext, string reflection)
    {
        string prompt = GeneratePrompt(aiManager, options, gameState, memoryContext, reflection);
        string response = await OpenAIService.Instance.GetResponse(prompt, aiManager.GetCharacterController().aiSettings, memoryContext, reflection);

        return ParseDecision(response, options);
    }

    private string GeneratePrompt(AIManager aiManager, List<string> options, GameState gameState, string memoryContext, string reflection)
    {
        UniversalCharacterController character = aiManager.GetCharacterController();
        string personalGoals = string.Join(", ", character.GetPersonalGoalTags());
        string completedMilestones = string.Join(", ", gameState.MilestoneCompletion.Where(kvp => kvp.Value).Select(kvp => kvp.Key));
        string incompleteMilestones = string.Join(", ", gameState.MilestoneCompletion.Where(kvp => !kvp.Value).Select(kvp => kvp.Key));

        return $"You are {character.characterName}, a {character.aiSettings.characterRole}. " +
               $"Your personality: {character.aiSettings.characterPersonality}\n" +
               $"Your personal goals: {personalGoals}\n" +
               $"Recent memories: {memoryContext}\n" +
               $"Your current reflection: {reflection}\n" +
               $"Current challenge: {gameState.CurrentChallenge.title}\n" +
               $"Completed milestones: {completedMilestones}\n" +
               $"Incomplete milestones: {incompleteMilestones}\n" +
               $"Current collective progress: {gameState.CollectiveProgress}%\n" +
               $"Your current score: {gameState.PlayerScores[character.characterName]}\n" +
               $"Time remaining: {Mathf.FloorToInt(gameState.RemainingTime / 60)} minutes\n\n" +
               "Given the following options, what action would you take? Consider your personality, personal goals, recent memories, current reflection, the current challenge, and the overall game state. " +
               "Respond with only the number of your chosen option.\n\n" +
               string.Join("\n", options.Select((option, index) => $"{index + 1}. {option}"));
    }

    private string ParseDecision(string response, List<string> options)
    {
        if (int.TryParse(response.Trim(), out int choiceIndex) && choiceIndex > 0 && choiceIndex <= options.Count)
        {
            return options[choiceIndex - 1];
        }
        return options[Random.Range(0, options.Count)];
    }

    private string GenerateCacheKey(AIManager aiManager, List<string> options, GameState gameState, string memoryContext, string reflection)
{
    string optionsHash = string.Join("|", options);
    string gameStateHash = $"{gameState.CurrentChallenge.title}|{gameState.CollectiveProgress}|{Mathf.FloorToInt(gameState.RemainingTime / 60)}";
    return $"{aiManager.GetCharacterController().characterName}|{optionsHash}|{gameStateHash}|{memoryContext}|{reflection}";
}
}