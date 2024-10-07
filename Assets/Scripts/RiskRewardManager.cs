using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq; // Added this line
using ProjectGrow.AI;

public class RiskRewardManager : MonoBehaviourPunCallbacks
{
    public static RiskRewardManager Instance { get; private set; }

    [SerializeField] private float defaultBaseSuccessRate = 0.8f; // Increased default success rate
    [SerializeField] private string playerRole = "Player";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
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

        if (isSuccessful)
        {
            // Successful action logic
            GameManager.Instance.UpdateGameState(character.characterName, $"Successfully completed {actionName}");

            // Update progress based on tags
            List<(string tag, float weight)> tagsWithWeights = TagSystem.GetTagsForAction(actionName);
            List<string> tags = tagsWithWeights.Select(t => t.tag).ToList();
            GameManager.Instance.UpdateProgressBasedOnTags(character.characterName, tags);

            // Check for Eureka moment if action involves collaboration
            if (character.IsCollaborating && !string.IsNullOrEmpty(character.currentCollabID))
            {
                List<UniversalCharacterController> collaborators = CollabManager.Instance.GetCollaborators(character.currentCollabID);
                if (collaborators != null && collaborators.Count >= 2)
                {
                    // Trigger Eureka moment
                    EurekaManager.Instance.TriggerEureka(collaborators, actionName);
                }
            }
        }
        else
        {
            // Action failed, but no penalties applied
            GameManager.Instance.UpdateGameState(character.characterName, $"Failed {actionName}");
            if (character.photonView.IsMine && LocationActionUI.Instance != null)
            {
                LocationActionUI.Instance.ShowOutcome("FAILURE");
            }
        }
    }

    private float CalculateSuccessRate(UniversalCharacterController character, string actionName)
    {
        // Simplified success rate calculation
        return defaultBaseSuccessRate; // Default success rate of 80%
    }
}
