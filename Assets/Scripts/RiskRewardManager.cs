using UnityEngine;
using Photon.Pun;

public class RiskRewardManager : MonoBehaviourPunCallbacks
{
    public static RiskRewardManager Instance { get; private set; }

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

    public void EvaluateActionOutcome(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        float successRate = CalculateSuccessRate(character, action);
        bool isSuccessful = Random.value < successRate;

        string outcome;
        int scoreChange;

        if (isSuccessful)
        {
            if (Random.value < 0.3f) // 30% chance of partial success
            {
                outcome = "PARTIAL SUCCESS";
                scoreChange = 5;
            }
            else
            {
                outcome = "SUCCESS";
                scoreChange = 10;
            }
        }
        else
        {
            outcome = "FAILURE";
            scoreChange = -5;
        }

        photonView.RPC("RPC_ApplyActionOutcome", RpcTarget.All, character.photonView.ViewID, outcome, scoreChange);
    }

    [PunRPC]
    public void RPC_ApplyActionOutcome(int characterViewID, string outcome, int scoreChange)
    {
        PhotonView characterView = PhotonView.Find(characterViewID);
        if (characterView == null) return;

        UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
        if (character == null) return;

        GameManager.Instance.UpdatePlayerScore(character.characterName, scoreChange);
        GameManager.Instance.UpdateGameState(character.characterName, $"{outcome}: {character.currentAction.actionName}");

        if (character.photonView.IsMine)
        {
            LocationActionUI.Instance.ShowOutcome(outcome);
        }

        // Update character's mental model
        AIManager aiManager = character.GetComponent<AIManager>();
        if (aiManager != null)
        {
            aiManager.AddMemory($"{outcome} on {character.currentAction.actionName} at {Time.time}");
            aiManager.UpdateEmotionalState(outcome == "SUCCESS" ? EmotionalState.Happy : (outcome == "FAILURE" ? EmotionalState.Sad : EmotionalState.Neutral));
        }
    }

    private float CalculateSuccessRate(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        float baseRate = action.baseSuccessRate;
        float roleBonus = (character.aiSettings.characterRole == action.requiredRole) ? 0.2f : 0f;
        
        // Add more modifiers here based on character stats, experience, etc.

        return Mathf.Clamp01(baseRate + roleBonus);
    }
}