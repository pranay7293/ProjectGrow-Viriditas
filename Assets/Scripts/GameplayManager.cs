using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public static GameplayManager Instance { get; private set; }

    [SerializeField] private float challengeDuration = 1800f; // 30 minutes
    private float remainingTime;

    private string currentChallenge;
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();

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
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeChallenge();
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateChallengeTime();
        }
    }

    private void InitializeChallenge()
    {
        currentChallenge = GetSelectedChallenge();
        remainingTime = challengeDuration;
        photonView.RPC("SyncChallenge", RpcTarget.All, currentChallenge, remainingTime);
    }

    private string GetSelectedChallenge()
    {
        // Implement logic to get the selected challenge from previous scenes
        return "Default Challenge";
    }

    private void UpdateChallengeTime()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            EndChallenge();
        }
    }

    private void EndChallenge()
    {
        // Implement challenge end logic
        photonView.RPC("ShowResults", RpcTarget.All);
    }

    [PunRPC]
    private void SyncChallenge(string challenge, float time)
    {
        currentChallenge = challenge;
        remainingTime = time;
        // Update UI or other necessary elements
    }

    [PunRPC]
    private void ShowResults()
    {
        // Implement results display logic
    }

    public void UpdatePlayerScore(string playerName, int score)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!playerScores.ContainsKey(playerName))
            {
                playerScores[playerName] = 0;
            }
            playerScores[playerName] += score;
            photonView.RPC("SyncPlayerScore", RpcTarget.All, playerName, playerScores[playerName]);
        }
    }

    [PunRPC]
    private void SyncPlayerScore(string playerName, int score)
    {
        playerScores[playerName] = score;
        // Update UI or other necessary elements
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public string GetCurrentChallenge()
    {
        return currentChallenge;
    }

    public Dictionary<string, int> GetPlayerScores()
    {
        return new Dictionary<string, int>(playerScores);
    }
}