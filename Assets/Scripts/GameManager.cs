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
    [SerializeField] private float challengeDuration = 1800f;
    [SerializeField] private int challengeGoal = 1000;
    [SerializeField] private float scenarioGenerationInterval = 300f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private Slider challengeProgressBar;
    [SerializeField] private TextMeshProUGUI emergentScenarioDisplay;
    [SerializeField] private TextMeshProUGUI subgoalsText;
    [SerializeField] private GameObject subgoalsPanel;
    [SerializeField] private Button toggleSubgoalsButton;

    [Header("Game Components")]
    [SerializeField] private EmergentScenarioGenerator scenarioGenerator;
    [SerializeField] private Transform[] spawnPoints;

    private float remainingTime;
    private string currentChallenge;
    private List<string> currentSubgoals = new List<string>();
    private Dictionary<string, int> subgoalProgress = new Dictionary<string, int>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private int collectiveScore = 0;
    private float lastScenarioTime;
    private List<string> recentPlayerActions = new List<string>();
    private Dictionary<string, UniversalCharacterController> spawnedCharacters = new Dictionary<string, UniversalCharacterController>();

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
        subgoalsPanel.SetActive(false);
        toggleSubgoalsButton.onClick.AddListener(ToggleSubgoalsPanel);
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
        GenerateSubgoals();
        photonView.RPC("SyncGameState", RpcTarget.All, currentChallenge, remainingTime, collectiveScore);
    }

    private string GetSelectedChallenge()
    {
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedChallengeTitle", out object challengeTitle))
        {
            return (string)challengeTitle;
        }
        else
        {
            return PlayerPrefs.GetString("SelectedChallengeTitle", "Default Challenge");
        }
    }

    private void GenerateSubgoals()
    {
        currentSubgoals.Clear();
        subgoalProgress.Clear();
        currentSubgoals.Add("Research the problem");
        currentSubgoals.Add("Develop a prototype");
        currentSubgoals.Add("Test the solution");
        currentSubgoals.Add("Implement the final version");

        foreach (string subgoal in currentSubgoals)
        {
            subgoalProgress[subgoal] = 0;
        }

        photonView.RPC("SyncSubgoals", RpcTarget.All, currentSubgoals.ToArray());
    }

    [PunRPC]
    private void SyncSubgoals(string[] subgoals)
    {
        currentSubgoals = new List<string>(subgoals);
        if (subgoalsPanel.activeSelf)
        {
            UpdateSubgoalDisplay();
        }
    }

    private void ToggleSubgoalsPanel()
    {
        subgoalsPanel.SetActive(!subgoalsPanel.activeSelf);
        if (subgoalsPanel.activeSelf)
        {
            UpdateSubgoalDisplay();
        }
    }

    private void UpdateSubgoalDisplay()
    {
        if (!subgoalsPanel.activeSelf) return;

        string subgoalText = "Current Subgoals:\n";
        foreach (string subgoal in currentSubgoals)
        {
            subgoalText += $"- {subgoal} (Progress: {subgoalProgress[subgoal]}/3)\n";
        }
        subgoalsText.text = subgoalText;
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
        string newScenario = await scenarioGenerator.GenerateScenario(currentChallenge, recentPlayerActions);
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

        UpdateSubgoalProgress(action);

        if (ActionContributesToChallenge(action))
        {
            UpdateCollectiveScore(10);
        }
    }

    private int EvaluateActionImpact(string action)
    {
        return Random.Range(1, 10);
    }

    private bool ActionContributesToChallenge(string action)
    {
        return action.ToLower().Contains(currentChallenge.ToLower());
    }

    private void UpdateSubgoalProgress(string action)
    {
        foreach (string subgoal in currentSubgoals)
        {
            if (action.ToLower().Contains(subgoal.ToLower()))
            {
                subgoalProgress[subgoal]++;
                if (subgoalProgress[subgoal] >= 3)
                {
                    CompleteSubgoal(subgoal);
                }
                break;
            }
        }
        UpdateSubgoalDisplay();
    }

    private void CompleteSubgoal(string subgoal)
    {
        currentSubgoals.Remove(subgoal);
        subgoalProgress.Remove(subgoal);
        UpdateCollectiveScore(50);
        photonView.RPC("NotifySubgoalCompletion", RpcTarget.All, subgoal);

        if (currentSubgoals.Count == 0)
        {
            GenerateSubgoals();
        }

        if (subgoalsPanel.activeSelf)
        {
            UpdateSubgoalDisplay();
        }
    }

    [PunRPC]
    private void NotifySubgoalCompletion(string completedSubgoal)
    {
        Debug.Log($"Subgoal completed: {completedSubgoal}");
        UpdateSubgoalDisplay();
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
    private void SyncGameState(string challenge, float time, int collective)
    {
        currentChallenge = challenge;
        remainingTime = time;
        collectiveScore = collective;
        challengeText.text = currentChallenge;
        UpdateTimer(time);
        UpdateScoreDisplay();
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
        float progress = (float)collectiveScore / challengeGoal;
        challengeProgressBar.value = progress;
        
        PlayerListManager playerListManager = FindObjectOfType<PlayerListManager>();
        if (playerListManager != null)
        {
            foreach (var kvp in playerScores)
            {
                Photon.Realtime.Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == kvp.Key);
                if (player != null)
                {
                    float playerProgress = (float)kvp.Value / challengeGoal;
                    playerListManager.UpdatePlayerProgress(player.ActorNumber, playerProgress);
                }
            }
        }
    }

    [PunRPC]
    private void ShowResults()
    {
        Debug.Log("Challenge ended. Displaying results...");
    }

    public GameState GetCurrentGameState()
    {
        return new GameState
        {
            CurrentChallenge = currentChallenge,
            CurrentSubgoals = new List<string>(currentSubgoals),
            CollectiveScore = collectiveScore
        };
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public string GetCurrentChallenge()
    {
        return currentChallenge;
    }

    public List<string> GetCurrentSubgoals()
    {
        return new List<string>(currentSubgoals);
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