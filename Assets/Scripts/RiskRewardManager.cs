using UnityEngine;
using Photon.Pun;

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

        if (isSuccessful)
        {
            outcome = Random.value < 0.3f ? "PARTIAL SUCCESS" : "SUCCESS";
            scoreChange = outcome == "PARTIAL SUCCESS" ? 5 : 10;
        }
        else
        {
            outcome = "FAILURE";
            scoreChange = -5;
        }

        ApplyActionOutcome(character, outcome, scoreChange, actionName);
    }

    private void ApplyActionOutcome(UniversalCharacterController character, string outcome, int scoreChange, string actionName)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("ApplyActionOutcome: GameManager.Instance is null");
            return;
        }

        GameManager.Instance.UpdatePlayerScore(character.characterName, scoreChange);
        GameManager.Instance.UpdateGameState(character.characterName, $"{outcome}: {actionName}");

        if (character.photonView.IsMine)
        {
            if (LocationActionUI.Instance != null)
            {
                LocationActionUI.Instance.ShowOutcome(outcome);
            }
            else
            {
                Debug.LogWarning("ApplyActionOutcome: LocationActionUI.Instance is null");
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

        // Display floating text for all characters
        Vector3 textPosition = character.transform.position + Vector3.up * 2f;
        string floatingText = $"{outcome}: {scoreChange}";
        FloatingTextManager.Instance.ShowFloatingText(floatingText, textPosition, outcome == "SUCCESS" ? FloatingTextType.Points : FloatingTextType.Milestone);
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
        if (CollabManager.Instance != null)
        {
            collabBonus = CollabManager.Instance.GetCollabSuccessBonus(actionName);
        }
        else
        {
            Debug.LogWarning("CalculateSuccessRate: CollabManager.Instance is null");
        }
        
        return Mathf.Clamp01(baseRate + roleBonus + collabBonus);
    }
}