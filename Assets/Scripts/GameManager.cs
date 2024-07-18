using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float challengeDuration = 1800f; // 30 minutes
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private Slider challengeProgressBar;
    [SerializeField] private TextMeshProUGUI collectiveScoreDisplay;
    [SerializeField] private TextMeshProUGUI emergentScenarioDisplay;
    [SerializeField] private EmergentScenarioGenerator scenarioGenerator;
    [SerializeField] private float scenarioGenerationInterval = 300f; // 5 minutes
    [SerializeField] private Transform[] spawnPoints;

    private float remainingTime;
    private string currentChallenge;
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

    private int challengeGoal = 1000; // This represents the target collective score to complete the challenge

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

        foreach (string characterName in CharacterSelectionManager.characterNames)
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
        // TODO: Implement logic to apply the scenario effects to the game state
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

        // Check if the action contributes to the challenge goal
        if (ActionContributesToChallenge(action))
        {
            UpdateCollectiveScore(10); // Additional score for challenge-related actions
        }
    }

    private int EvaluateActionImpact(string action)
    {
        // TODO: Implement more sophisticated action impact evaluation
        return Random.Range(1, 10);
    }

    private bool ActionContributesToChallenge(string action)
    {
        // Implement logic to determine if the action contributes to the current challenge
        // This could involve keyword matching or more sophisticated NLP techniques
        return action.ToLower().Contains(currentChallenge.ToLower());
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
        collectiveScoreDisplay.text = "Community Score: " + collectiveScore;
        challengeProgressBar.value = (float)collectiveScore / challengeGoal;
        
        // Update individual player scores in PlayerListManager
        PlayerListManager playerListManager = FindObjectOfType<PlayerListManager>();
        if (playerListManager != null)
        {
            foreach (var kvp in playerScores)
            {
                Photon.Realtime.Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == kvp.Key);
                if (player != null)
                {
                    float progress = (float)kvp.Value / challengeGoal;
                    playerListManager.UpdatePlayerProgress(player.ActorNumber, progress);
                }
            }
        }
    }

    [PunRPC]
    private void ShowResults()
    {
        Debug.Log("Challenge ended. Displaying results...");
        // TODO: Implement results display logic
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