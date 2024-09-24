using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using ProjectGrow.AI;

public class RiskRewardManager : MonoBehaviourPunCallbacks
{
    public static RiskRewardManager Instance { get; private set; }

    [SerializeField] private float defaultBaseSuccessRate = 0.7f;
    [SerializeField] private string playerRole = "Player";

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

    public void EvaluateActionOutcome(UniversalCharacterController character, string actionName)
    {
        if (character == null)
        {
            Debug.LogWarning("EvaluateActionOutcome: Character is null");
            return;
        }

        float successRate = CalculateSuccessRate(character, actionName);
        bool isSuccessful = Random.value < successRate;

        string outcome;
        int scoreChange;
        string reason;

        if (isSuccessful)
        {
            outcome = Random.value < 0.3f ? "PARTIAL SUCCESS" : "SUCCESS";
            scoreChange = ScoreConstants.GetActionPoints(character.currentAction.duration);
            reason = $"{outcome} on {actionName}";

            // Check for Eureka if collaborating
            if (character.IsCollaborating && !string.IsNullOrEmpty(character.currentCollabID))
            {
                if (CollabManager.Instance != null)
                {
                    List<UniversalCharacterController> collaborators = CollabManager.Instance.GetCollaborators(character.currentCollabID);
                    if (EurekaManager.Instance != null)
                    {
                        EurekaManager.Instance.CheckForEureka(collaborators, actionName);
                    }
                    else
                    {
                        Debug.LogWarning("EvaluateActionOutcome: EurekaManager.Instance is null");
                    }
                }
                else
                {
                    Debug.LogWarning("EvaluateActionOutcome: CollabManager.Instance is null");
                }
            }
        }
        else
        {
            outcome = "FAILURE";
            scoreChange = ScoreConstants.GetActionFailurePoints(character.currentAction.duration);
            reason = $"Failed {actionName}";
        }

        GameManager.Instance.UpdatePlayerScore(character.characterName, scoreChange, reason, new List<string> { actionName, outcome });
        GameManager.Instance.UpdateGameState(character.characterName, $"{outcome}: {actionName}");

        if (character.photonView.IsMine)
        {
            if (LocationActionUI.Instance != null)
            {
                LocationActionUI.Instance.ShowOutcome(outcome);
            }
            else
            {
                Debug.LogWarning("EvaluateActionOutcome: LocationActionUI.Instance is null");
            }
        }

        // Update character's mental model only for AI characters
        if (!character.IsPlayerControlled)
        {
            AIManager aiManager = character.GetComponent<AIManager>();
            if (aiManager != null)
            {
                aiManager.AddMemory($"{outcome} on {actionName} at {Time.time}");
                aiManager.UpdateEmotionalState(outcome == "SUCCESS" ? EmotionalState.Happy : (outcome == "FAILURE" ? EmotionalState.Sad : EmotionalState.Neutral));
            }
        }
    }

    private float CalculateSuccessRate(UniversalCharacterController character, string actionName)
    {
        float baseRate = defaultBaseSuccessRate;
        float roleBonus = 0f;

        if (character.aiSettings != null)
        {
            roleBonus = (character.aiSettings.characterRole == playerRole) ? 0.2f : 0f;
        }
        else
        {
            Debug.LogWarning($"CalculateSuccessRate: aiSettings is null for character {character.characterName}. Using default player role.");
        }

        float collabBonus = 0f;
        if (character.IsCollaborating && !string.IsNullOrEmpty(character.currentCollabID))
        {
            if (CollabManager.Instance != null)
            {
                collabBonus = CollabManager.Instance.GetCollabSuccessBonus(character.currentCollabID);
            }
            else
            {
                Debug.LogWarning("CalculateSuccessRate: CollabManager.Instance is null");
            }
        }
        else
        {
            
        }

        return Mathf.Clamp01(baseRate + roleBonus + collabBonus);
    }
}
