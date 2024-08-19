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
            Dictionary<string, int> playerScores = (Dictionary<string, int>)playerScoresObj;

            var sortedScores = playerScores.OrderByDescending(pair => pair.Value);

            foreach (var score in sortedScores)
            {
                GameObject scoreItem = Instantiate(playerScoreItemPrefab, leaderboardContent);
                TextMeshProUGUI[] texts = scoreItem.GetComponentsInChildren<TextMeshProUGUI>();
                texts[0].text = score.Key; // Player name
                texts[1].text = score.Value.ToString(); // Score
            }
        }
        else
        {
            Debug.LogError("Failed to retrieve player scores from room properties.");
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