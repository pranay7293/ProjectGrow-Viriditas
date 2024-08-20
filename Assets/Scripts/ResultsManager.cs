using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class ResultsManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject playerScoreItemPrefab;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private TextMeshProUGUI noScoresText;

    private void Start()
    {
        PopulateLeaderboard();
        returnToLobbyButton.onClick.AddListener(ReturnToLobby);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToLobby();
        }
    }

    private void PopulateLeaderboard()
    {
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("PlayerScores", out object playerScoresObj))
        {
            Dictionary<string, int> playerScores = playerScoresObj as Dictionary<string, int>;

            if (playerScores != null && playerScores.Count > 0)
            {
                var sortedScores = playerScores.OrderByDescending(pair => pair.Value);

                foreach (var score in sortedScores)
                {
                    GameObject scoreItem = Instantiate(playerScoreItemPrefab, leaderboardContent);
                    TextMeshProUGUI[] texts = scoreItem.GetComponentsInChildren<TextMeshProUGUI>();
                    texts[0].text = score.Key; // Player name
                    texts[1].text = score.Value.ToString(); // Score
                }

                if (noScoresText != null)
                {
                    noScoresText.gameObject.SetActive(false);
                }
            }
            else
            {
                ShowNoScoresMessage();
            }
        }
        else
        {
            ShowNoScoresMessage();
        }
    }

    private void ShowNoScoresMessage()
    {
        if (noScoresText != null)
        {
            noScoresText.gameObject.SetActive(true);
            noScoresText.text = "No scores available. The game may have ended prematurely.";
        }
        else
        {
            Debug.LogError("No Scores Text is not assigned in the inspector.");
        }
    }

    private void ReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("ChallengeLobby");
    }
}