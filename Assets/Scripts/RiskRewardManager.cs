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

        photonView.RPC("RPC_ApplyActionOutcome", RpcTarget.All, character.photonView.ViewID, outcome, scoreChange, action.actionName);
    }

    [PunRPC]
    public void RPC_ApplyActionOutcome(int characterViewID, string outcome, int scoreChange, string actionName)
    {
        PhotonView characterView = PhotonView.Find(characterViewID);
        if (characterView == null) return;

        UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
        if (character == null) return;

        GameManager.Instance.UpdatePlayerScore(character.characterName, scoreChange);
        GameManager.Instance.UpdateGameState(character.characterName, $"{outcome}: {actionName}");

        if (character.photonView.IsMine)
        {
            LocationActionUI.Instance.ShowOutcome(outcome);
        }

        // Update character's mental model
        AIManager aiManager = character.GetComponent<AIManager>();
        if (aiManager != null)
        {
            aiManager.AddMemory($"{outcome} on {actionName} at {Time.time}");
            aiManager.UpdateEmotionalState(outcome == "SUCCESS" ? EmotionalState.Happy : (outcome == "FAILURE" ? EmotionalState.Sad : EmotionalState.Neutral));
        }
    }

    private float CalculateSuccessRate(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        float baseRate = action.baseSuccessRate;
        float roleBonus = (character.aiSettings.characterRole == action.requiredRole) ? 0.2f : 0f;
        float collabBonus = CollabManager.Instance.GetCollabSuccessBonus(action.actionName);
        
        return Mathf.Clamp01(baseRate + roleBonus + collabBonus);
    }
}