using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public static GameplayManager Instance { get; private set; }

    [SerializeField] private float challengeDuration = 1800f; // 30 minutes
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private EmergentScenarioGenerator scenarioGenerator;
    [SerializeField] private float scenarioGenerationInterval = 300f; // 5 minutes
    private float lastScenarioTime;

    private List<string> recentPlayerActions = new List<string>();

    private float remainingTime;
    private string currentChallenge;
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private int collectiveScore = 0;

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
            CheckForNewScenario();
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
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedChallengeTitle", out object challengeTitle))
        {
            return (string)challengeTitle;
        }
        return "Default Challenge";
    }

    private void UpdateChallengeTime()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            EndChallenge();
        }
        else
        {
            photonView.RPC("UpdateTimer", RpcTarget.All, remainingTime);
        }
    }

     private async void CheckForNewScenario()
    {
        if (Time.time - lastScenarioTime >= scenarioGenerationInterval)
        {
            lastScenarioTime = Time.time;
            string newScenario = await scenarioGenerator.GenerateScenario(currentChallenge, recentPlayerActions);
            scenarioGenerator.ApplyScenario(newScenario);
            recentPlayerActions.Clear();
        }
    }

    public void AddPlayerAction(string action)
    {
        recentPlayerActions.Add(action);
        if (recentPlayerActions.Count > 5) // Keep only the 5 most recent actions
        {
            recentPlayerActions.RemoveAt(0);
        }
    }

    [PunRPC]
    private void RPC_NotifyNewScenario(string scenario)
    {
        // TODO: Display the new scenario to players
        Debug.Log($"New scenario: {scenario}");
    }

    [PunRPC]
    private void UpdateTimer(float time)
    {
        remainingTime = time;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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
        challengeText.text = currentChallenge;
        UpdateTimer(time);
    }

    [PunRPC]
    private void ShowResults()
    {
        // Implement results display logic
        Debug.Log("Challenge ended. Displaying results...");
        // TODO: Show a results screen with player scores and collective score
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

    public void UpdateCollectiveScore(int points)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            collectiveScore += points;
            photonView.RPC("SyncCollectiveScore", RpcTarget.All, collectiveScore);
        }
    }

    [PunRPC]
    private void SyncPlayerScore(string playerName, int score)
    {
        playerScores[playerName] = score;
        // TODO: Update UI to show player scores
    }

    [PunRPC]
    private void SyncCollectiveScore(int score)
    {
        collectiveScore = score;
        // TODO: Update UI to show collective score
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

    public int GetCollectiveScore()
    {
        return collectiveScore;
    }
}