using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float challengeDuration = 900f; // 15 minutes
    [SerializeField] private float scenarioGenerationInterval = 300f; // 5 minutes

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private Slider challengeProgressBar;
    [SerializeField] private TextMeshProUGUI emergentScenarioDisplay;
    [SerializeField] private GameObject milestonesPanel;
    [SerializeField] private Button toggleMilestonesButton;
    [SerializeField] private TextMeshProUGUI milestonesText;

    [Header("Game Components")]
    [SerializeField] private EmergentScenarioGenerator scenarioGenerator;
    [SerializeField] private Transform[] spawnPoints;

    private float remainingTime;
    private ChallengeData currentChallenge;
    private Dictionary<string, bool> milestoneCompletion = new Dictionary<string, bool>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private int collectiveScore = 0;
    private float lastScenarioTime;
    private List<string> recentPlayerActions = new List<string>();
    private Dictionary<string, UniversalCharacterController> spawnedCharacters = new Dictionary<string, UniversalCharacterController>();
    private Dictionary<string, int> playerInsights = new Dictionary<string, int>();

    private readonly Dictionary<string, string> characterLocations = new Dictionary<string, string>
    {
        {"Aspen Rodriguez", "Biofoundry"},
        {"Astra Kim", "Space Center"},
        {"Celeste Dubois", "Gallery"},
        {"Dr. Cobalt Johnson", "Research Lab"},
        {"Dr. Eden Kapoor", "Medical Bay"},
        {"Dr. Flora Tremblay", "Think Tank"},
        {"Indigo", "Maker Space"},
        {"Lilith Fernandez", "Media Center"},
        {"River Osei", "Sound Studio"},
        {"Sierra Nakamura", "Innovation Hub"}
    };

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
            InitializeGame();
        }
        milestonesPanel.SetActive(false);
        toggleMilestonesButton.onClick.AddListener(ToggleMilestonesPanel);
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateGameTime();
            CheckForNewScenario();
        }
    }

    public void InitializeGame()
    {
        SpawnCharacters();
        InitializeChallenge();
    }

    private void SpawnCharacters()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
            {
                string characterName = (string)selectedCharacter;
                SpawnCharacter(characterName, true, player);
            }
        }

        foreach (string characterName in CharacterSelectionManager.characterFullNames)
        {
            if (!spawnedCharacters.ContainsKey(characterName))
            {
                SpawnCharacter(characterName, false, null);
            }
        }
    }

    private void SpawnCharacter(string characterName, bool isPlayerControlled, Photon.Realtime.Player player = null)
    {
        if (!characterLocations.TryGetValue(characterName, out string locationName))
        {
            Debug.LogError($"No location found for character: {characterName}");
            return;
        }

        Transform spawnPoint = GetSpawnPointByName(locationName);
        if (spawnPoint == null)
        {
            Debug.LogError($"No spawn point found for location: {locationName}");
            return;
        }

        Vector3 spawnPosition = spawnPoint.position;
        GameObject characterGO = PhotonNetwork.Instantiate("Character-" + characterName, spawnPosition, Quaternion.identity);

        UniversalCharacterController character = characterGO.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            if (isPlayerControlled && player != null)
            {
                characterGO.GetComponent<PhotonView>().TransferOwnership(player);
            }
            Color characterColor = character.characterColor;
            character.photonView.RPC("Initialize", RpcTarget.All, characterName, isPlayerControlled, characterColor.r, characterColor.g, characterColor.b);
            spawnedCharacters[characterName] = character;

            if (!isPlayerControlled)
            {
                AIManager aiManager = characterGO.GetComponent<AIManager>();
                if (aiManager != null)
                {
                    aiManager.Initialize(character);
                }
                else
                {
                    Debug.LogError($"AIManager component not found on spawned character {characterName}");
                }
            }
        }
        else
        {
            Debug.LogError($"UniversalCharacterController component not found on spawned character {characterName}");
        }
    }

    private Transform GetSpawnPointByName(string locationName)
    {
        return spawnPoints.FirstOrDefault(sp => sp.name == locationName);
    }

    private void InitializeChallenge()
    {
        currentChallenge = GetSelectedChallenge();
        remainingTime = challengeDuration;
        lastScenarioTime = 0f;
        recentPlayerActions.Clear();
        collectiveScore = 0;
        
        milestoneCompletion.Clear();
        foreach (var milestone in currentChallenge.milestones)
        {
            milestoneCompletion[milestone] = false;
        }

        photonView.RPC("SyncGameState", RpcTarget.All, JsonUtility.ToJson(currentChallenge), remainingTime, collectiveScore, playerScores);
    }

    private ChallengeData GetSelectedChallenge()
    {
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedChallengeTitle", out object challengeTitle))
        {
            return ChallengeDatabase.GetChallenge((string)challengeTitle);
        }
        else
        {
            return ChallengeDatabase.GetChallenge(PlayerPrefs.GetString("SelectedChallengeTitle", "Default Challenge"));
        }
    }

    private void ToggleMilestonesPanel()
    {
        milestonesPanel.SetActive(!milestonesPanel.activeSelf);
        if (milestonesPanel.activeSelf)
        {
            UpdateMilestonesDisplay();
        }
    }

    private void UpdateMilestonesDisplay()
    {
        string milestonesStatus = "Challenge Milestones:\n\n";
        foreach (var milestone in currentChallenge.milestones)
        {
            string status = milestoneCompletion[milestone] ? "Completed" : "In Progress";
            milestonesStatus += $"- {milestone}: {status}\n";
        }
        milestonesText.text = milestonesStatus;
    }

    private void UpdateGameTime()
    {
        if (!PhotonNetwork.IsMasterClient) return;

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

    [PunRPC]
    private void UpdateTimer(float time)
    {
        remainingTime = time;
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void CheckForNewScenario()
    {
        if (Time.time - lastScenarioTime >= scenarioGenerationInterval)
        {
            lastScenarioTime = Time.time;
            GenerateNewScenario();
        }
    }

    private async void GenerateNewScenario()
    {
        string newScenario = await scenarioGenerator.GenerateScenario(currentChallenge.title, recentPlayerActions);
        ApplyScenario(newScenario);
        recentPlayerActions.Clear();
    }

    private void ApplyScenario(string scenario)
    {
        photonView.RPC("NotifyNewScenario", RpcTarget.All, scenario);
    }

    [PunRPC]
    private void NotifyNewScenario(string scenario)
    {
        emergentScenarioDisplay.text = scenario;
    }

    public void AddPlayerAction(string action)
    {
        recentPlayerActions.Add(action);
        if (recentPlayerActions.Count > 5)
        {
            recentPlayerActions.RemoveAt(0);
        }
    }

    public void UpdateGameState(string characterName, string action)
    {
        Debug.Log($"{characterName} performed action: {action}");
        int scoreIncrease = EvaluateActionImpact(action);
        UpdateCollectiveScore(scoreIncrease);
        AddPlayerAction(action);

        if (ActionContributesToChallenge(action))
        {
            UpdateCollectiveScore(10);
        }

        CheckMilestoneProgress(action);
    }

    private int EvaluateActionImpact(string action)
    {
        return Random.Range(1, 10);
    }

    private bool ActionContributesToChallenge(string action)
    {
        return action.ToLower().Contains(currentChallenge.title.ToLower());
    }

    private void CheckMilestoneProgress(string action)
    {
        foreach (var milestone in currentChallenge.milestones)
        {
            if (!milestoneCompletion[milestone] && action.ToLower().Contains(milestone.ToLower()))
            {
                CompleteMilestone(milestone);
                break;
            }
        }
    }

    public void CompleteMilestone(string milestone)
    {
        if (milestoneCompletion[milestone] == false)
        {
            milestoneCompletion[milestone] = true;
            UpdateCollectiveScore(100);
            photonView.RPC("SyncMilestoneCompletion", RpcTarget.All, milestone);
            CheckAllMilestonesCompleted();
        }
    }

    [PunRPC]
    private void SyncMilestoneCompletion(string milestone)
    {
        milestoneCompletion[milestone] = true;
        UpdateMilestonesDisplay();
    }

    public void GenerateInsight(string player1, string player2)
    {
        playerInsights[player1]++;
        playerInsights[player2]++;
        UpdatePlayerScore(player1, 10);
        UpdatePlayerScore(player2, 10);
    }

    public void UseInsight(string playerName)
    {
        if (playerInsights[playerName] > 0)
        {
            playerInsights[playerName]--;
            UpdatePlayerScore(playerName, 20);
            // Implement logic to boost progress on milestones or personal objectives
        }
    }

    private void CheckAllMilestonesCompleted()
    {
        if (milestoneCompletion.All(m => m.Value))
        {
            UpdateCollectiveScore(500); // Bonus for completing all milestones
            EndChallenge();
        }
    }

    public void UpdateEmergentScenario(string scenario)
    {
    emergentScenarioDisplay.text = scenario;
    // Implement any additional logic for handling the new scenario
    }

    public bool IsMilestoneCompleted(string milestone)
    {
    return milestoneCompletion.ContainsKey(milestone) && milestoneCompletion[milestone];
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

    private void EndChallenge()
    {
        photonView.RPC("ShowResults", RpcTarget.All);
    }

    [PunRPC]
    private void SyncGameState(string challengeJson, float time, int collective, Dictionary<string, int> scores)
    {
        currentChallenge = JsonUtility.FromJson<ChallengeData>(challengeJson);
        remainingTime = time;
        collectiveScore = collective;
        playerScores = new Dictionary<string, int>(scores);
        challengeText.text = currentChallenge.title;
        UpdateTimer(time);
        UpdateScoreDisplay();
        UpdateMilestonesDisplay();
    }

    [PunRPC]
    private void SyncPlayerScore(string playerName, int score)
    {
        playerScores[playerName] = score;
        UpdateScoreDisplay();
    }

    [PunRPC]
    private void SyncCollectiveScore(int score)
    {
        collectiveScore = score;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        float progress = (float)collectiveScore / currentChallenge.goalScore;
        challengeProgressBar.value = progress;
        
        PlayerListManager playerListManager = FindObjectOfType<PlayerListManager>();
        if (playerListManager != null)
        {
            foreach (var kvp in playerScores)
            {
                Photon.Realtime.Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == kvp.Key);
                if (player != null)
                {
                    float playerProgress = (float)kvp.Value / currentChallenge.goalScore;
                    playerListManager.UpdatePlayerProgress(player.ActorNumber, playerProgress);
                }
            }
        }
    }

    [PunRPC]
    private void ShowResults()
    {
        Debug.Log("Challenge ended. Displaying results...");
        // Implement results display logic
    }

    public GameState GetCurrentGameState()
    {
        return new GameState(
            currentChallenge,
            collectiveScore,
            new Dictionary<string, int>(playerScores),
            remainingTime
        );
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public ChallengeData GetCurrentChallenge()
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

    public UniversalCharacterController GetCharacterByName(string characterName)
    {
        if (spawnedCharacters.TryGetValue(characterName, out UniversalCharacterController character))
        {
            return character;
        }
        return null;
    }

    public List<UniversalCharacterController> GetAllCharacters()
    {
        return new List<UniversalCharacterController>(spawnedCharacters.Values);
    }
}