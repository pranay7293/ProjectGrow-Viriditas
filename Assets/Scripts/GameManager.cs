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
    [SerializeField] private float minimumPlayTime = 60f; // 1 minute minimum play time

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private GameObject milestonesDisplay;
    [SerializeField] private TextMeshProUGUI[] milestoneTexts;
    [SerializeField] private CustomCheckbox[] milestoneCheckboxes;
    [SerializeField] private ChallengeProgressUI challengeProgressUI;
    [SerializeField] private EmergentScenarioNotification emergentScenarioNotification;
    [SerializeField] public DialogueRequestUI dialogueRequestUI;

    [Header("Game Components")]
    [SerializeField] private EmergentScenarioGenerator scenarioGenerator;
    [SerializeField] private EmergentScenarioUI scenarioUI;
    [SerializeField] private Transform[] spawnPoints;

    private float remainingTime;
    private float gameStartTime;
    private ChallengeData currentChallenge;
    private HubData currentHub;
    private Dictionary<string, bool> milestoneCompletion = new Dictionary<string, bool>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private Dictionary<string, float[]> playerPersonalProgress = new Dictionary<string, float[]>();
    private List<string> recentPlayerActions = new List<string>();
    private Dictionary<string, UniversalCharacterController> spawnedCharacters = new Dictionary<string, UniversalCharacterController>();
    private Dictionary<string, int> playerEurekas = new Dictionary<string, int>();

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
        {"River Osei", "Economics Center"},
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
        milestonesDisplay.SetActive(false);
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateGameTime();
            CheckForEmergentScenario();

            if (ShouldEndGame())
            {
                EndChallenge();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleMilestonesDisplay();
        }
    }

    public void InitializeGame()
    {
        SpawnCharacters();
        InitializeChallenge();
        PlayerProfileManager.Instance.InitializeProfiles();
        gameStartTime = Time.time;
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

            playerScores[characterName] = 0;
            playerPersonalProgress[characterName] = new float[3] { 0f, 0f, 0f };
            playerEurekas[characterName] = 0;
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
        currentHub = GetSelectedHub();
        if (currentChallenge == null || currentHub == null)
        {
            Debug.LogError("Failed to initialize challenge or hub.");
            return;
        }

        remainingTime = challengeDuration;
        recentPlayerActions.Clear();
        
        milestoneCompletion.Clear();
        for (int i = 0; i < currentChallenge.milestones.Count; i++)
        {
            string milestone = currentChallenge.milestones[i];
            milestoneCompletion[milestone] = false;
            
            if (i < milestoneTexts.Length)
            {
                milestoneTexts[i].text = $"Milestone {i + 1}: {milestone}";
            }
            if (i < milestoneCheckboxes.Length)
            {
                milestoneCheckboxes[i].IsChecked = false;
            }
        }

        challengeProgressUI.Initialize(currentHub.hubColor);

        if (challengeText != null)
        {
            challengeText.text = currentChallenge.title;
        }
        else
        {
            Debug.LogError("Challenge Text UI element is not assigned in GameManager.");
        }

        SerializableChallengeData serializableChallenge = new SerializableChallengeData(currentChallenge);
        string challengeJson = JsonUtility.ToJson(serializableChallenge);
        photonView.RPC("SyncGameState", RpcTarget.All, challengeJson, remainingTime, playerScores);
    }

    private HubData GetSelectedHub()
    {
        int selectedHubIndex = PlayerPrefs.GetInt("SelectedHubIndex", 0);
        HubData[] allHubs = Resources.LoadAll<HubData>("Hubs");
        
        if (selectedHubIndex >= 0 && selectedHubIndex < allHubs.Length)
        {
            return allHubs[selectedHubIndex];
        }
        else
        {
            Debug.LogError("Selected hub index out of range. Using default hub.");
            return allHubs[0];
        }
    }
    
    private ChallengeData GetSelectedChallenge()
    {
        string challengeTitle = PlayerPrefs.GetString("SelectedChallengeTitle", "Default Challenge");
        
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedChallengeTitle", out object title))
        {
            challengeTitle = (string)title;
        }
        
        Debug.Log($"Attempting to get challenge: {challengeTitle}");
        
        ChallengeData[] allChallenges = Resources.LoadAll<ChallengeData>("Challenges");
        
        ChallengeData selectedChallenge = allChallenges.FirstOrDefault(c => c.title == challengeTitle);
        
        if (selectedChallenge == null)
        {
            Debug.LogError($"Failed to load challenge: {challengeTitle}. Using first available challenge.");
            selectedChallenge = allChallenges.FirstOrDefault();
            
            if (selectedChallenge == null)
            {
                Debug.LogError("No challenges found in Resources/Challenges. Please ensure challenge assets exist.");
            }
        }
        
        return selectedChallenge;
    }

    public void ToggleMilestonesDisplay()
    {
        milestonesDisplay.SetActive(!milestonesDisplay.activeSelf);
        if (milestonesDisplay.activeSelf)
        {
            UpdateMilestonesDisplay();
        }
    }

    private void UpdateMilestonesDisplay()
    {
        for (int i = 0; i < currentChallenge.milestones.Count; i++)
        {
            if (i < milestoneTexts.Length && i < milestoneCheckboxes.Length)
            {
                string milestone = currentChallenge.milestones[i];
                milestoneTexts[i].text = $"Milestone {i + 1}: {milestone}";
                milestoneCheckboxes[i].IsChecked = milestoneCompletion[milestone];
            }
        }
    }

    private void UpdateGameTime()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        remainingTime -= Time.deltaTime;
        photonView.RPC("UpdateTimer", RpcTarget.All, remainingTime);
    }

    public float GetChallengeDuration()
    {
        return challengeDuration;
    }

    private bool ShouldEndGame()
    {
        float elapsedTime = Time.time - gameStartTime;
        bool timeUp = remainingTime <= 0;
        bool allMilestonesCompleted = milestoneCompletion.Count > 0 && milestoneCompletion.All(m => m.Value);
        bool minimumTimeMet = elapsedTime >= minimumPlayTime;

        return (timeUp || allMilestonesCompleted) && minimumTimeMet;
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

    private bool hasTriggered5MinScenario = false;
    private bool hasTriggered10MinScenario = false;

    private void CheckForEmergentScenario()
    {
        float gameTime = Time.time - gameStartTime;
        
        if (!hasTriggered5MinScenario && gameTime >= 300f && gameTime < 301f)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"Triggering 5-minute scenario at {gameTime}");
                TriggerEmergentScenario();
                hasTriggered5MinScenario = true;
            }
        }
        else if (!hasTriggered10MinScenario && gameTime >= 600f && gameTime < 601f)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"Triggering 10-minute scenario at {gameTime}");
                TriggerEmergentScenario();
                hasTriggered10MinScenario = true;
            }
        }
    }

    private async void TriggerEmergentScenario()
    {
        if (scenarioGenerator == null)
        {
            Debug.LogError("EmergentScenarioGenerator not found in the scene.");
            return;
        }

        Debug.Log($"TriggerEmergentScenario called at {Time.time}");

        var scenarios = await scenarioGenerator.GenerateScenarios(currentChallenge.title, GetRecentPlayerActions());
        if (scenarios != null && scenarios.Count > 0)
        {
            photonView.RPC("RPC_DisplayEmergentScenarios", RpcTarget.All, scenarios.Select(s => s.description).ToArray());
        }
        else
        {
            Debug.LogError("Failed to generate scenarios.");
        }
    }

    [PunRPC]
    private void RPC_DisplayEmergentScenarios(string[] scenarioDescriptions)
    {
        Debug.Log($"RPC_DisplayEmergentScenarios called at {Time.time}");

        if (scenarioUI == null)
        {
            scenarioUI = FindObjectOfType<EmergentScenarioUI>(true);
        }

        if (scenarioUI != null)
        {
            scenarioUI.DisplayScenarios(scenarioDescriptions.ToList());
        }
        else
        {
            Debug.LogError("EmergentScenarioUI not found. Cannot display scenarios.");
        }
    }

    public void AddPlayerAction(string action)
    {
        recentPlayerActions.Add(action);
        if (recentPlayerActions.Count > 5)
        {
            recentPlayerActions.RemoveAt(0);
        }
    }

    public List<string> GetRecentPlayerActions()
    {
        return new List<string>(recentPlayerActions);
    }

    public void UpdateGameState(string characterName, string action, bool isEmergentScenario = false)
    {
        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(action))
        {
            Debug.LogWarning("Invalid character name or action in UpdateGameState");
            return;
        }

        if (isEmergentScenario)
        {
            Debug.Log($"Emergent Scenario: {action}");
            photonView.RPC("RPC_ImplementEmergentScenario", RpcTarget.All, action);
        }
        else
        {
            Debug.Log($"{characterName} performed action: {action}");
            int scoreIncrease = EvaluateActionImpact(action);
            AddPlayerAction(action);
            ActionLogManager.Instance.LogAction(characterName, action);

            UpdateMilestoneProgress(characterName, action);
            UpdatePlayerScore(characterName, scoreIncrease);

            UniversalCharacterController character = GetCharacterByName(characterName);
            if (character != null)
            {
                AIManager aiManager = character.GetComponent<AIManager>();
                if (aiManager != null)
                {
                    LocationManager.LocationAction locationAction = new LocationManager.LocationAction { actionName = action };
                    aiManager.ConsiderCollaboration(locationAction);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_ImplementEmergentScenario(string scenario)
    {
        ActionLogManager.Instance.LogAction("SYSTEM", $"Emergent Scenario: {scenario}");

        if (emergentScenarioNotification != null)
        {
            emergentScenarioNotification.DisplayNotification(scenario);
        }
        else
        {
            Debug.LogWarning("EmergentScenarioNotification is not assigned in GameManager");
        }

        ResetPlayerPositions();
    }

    public void HandleCollabCompletion(string actionName, List<UniversalCharacterController> collaborators)
    {
        foreach (var collaborator in collaborators)
        {
            UpdatePlayerScore(collaborator.characterName, ScoreConstants.COLLABORATION_BONUS);
            
            if (Random.value < ScoreConstants.EUREKA_CHANCE)
            {
                collaborator.IncrementEurekaCount();
                UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS);
            }
        }

        foreach (var collaborator in collaborators)
        {
            collaborator.EndCollab(actionName);
        }
    }

    private int EvaluateActionImpact(string action)
    {
        if (action.Length <= 15)
            return ScoreConstants.SIMPLE_ACTION_POINTS;
        else if (action.Length <= 30)
            return ScoreConstants.MEDIUM_ACTION_POINTS;
        else
            return ScoreConstants.COMPLEX_ACTION_POINTS;
    }

    public void CompleteMilestone(string characterName, string milestone, int milestoneIndex)
    {
        if (milestoneCompletion[milestone] == false)
        {
            milestoneCompletion[milestone] = true;
            UpdatePlayerScore(characterName, ScoreConstants.MILESTONE_COMPLETION_BONUS);
            UpdatePersonalProgress(characterName, milestoneIndex, 1f);
            photonView.RPC("SyncMilestoneCompletion", RpcTarget.All, milestone, characterName, milestoneIndex);
            CheckAllMilestonesCompleted();
        }
    }

    [PunRPC]
    private void SyncMilestoneCompletion(string milestone, string characterName, int milestoneIndex)
    {
        milestoneCompletion[milestone] = true;
        UpdatePersonalProgress(characterName, milestoneIndex, 1f);
        if (milestoneIndex < milestoneCheckboxes.Length)
        {
            milestoneCheckboxes[milestoneIndex].IsChecked = true;
        }
        UpdateMilestonesDisplay();

        UniversalCharacterController character = GetCharacterByName(characterName);
        if (character != null)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2.5f;
            FloatingTextManager.Instance.ShowFloatingText("Milestone Completed!", textPosition, FloatingTextType.Milestone);
        }
    }

    public string CompleteRandomMilestone(string eurekaDescription)
    {
        ChallengeData currentChallenge = GetCurrentChallenge();
        List<string> incompleteMilestones = currentChallenge.milestones.FindAll(m => !IsMilestoneCompleted(m));
        
        if (incompleteMilestones.Count > 0)
        {
            string milestone = incompleteMilestones[Random.Range(0, incompleteMilestones.Count)];
            CompleteMilestone("Eureka", milestone, currentChallenge.milestones.IndexOf(milestone));
            ActionLogManager.Instance.LogAction("Eureka", $"Milestone completed: {milestone} - {eurekaDescription}");
            return milestone;
        }
        return "No milestone completed";
    }

    private void CheckAllMilestonesCompleted()
    {
        if (milestoneCompletion.Count > 0 && milestoneCompletion.All(m => m.Value))
        {
            EndChallenge();
        }
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
            photonView.RPC("SyncPlayerScore", RpcTarget.All, playerName, playerScores[playerName], score);
        }
    }

    [PunRPC]
    private void SyncPlayerScore(string playerName, int totalScore, int scoreGain)
    {
        playerScores[playerName] = totalScore;
        UpdateScoreDisplay();

        UniversalCharacterController character = GetCharacterByName(playerName);
        if (character != null)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            string scoreText = (scoreGain >= 0 ? "+" : "") + scoreGain;
            FloatingTextManager.Instance.ShowFloatingText(scoreText, textPosition, FloatingTextType.Points);
        }
    }

    private void UpdatePersonalProgress(string characterName, int goalIndex, float progress)
    {
        if (playerPersonalProgress.TryGetValue(characterName, out float[] personalProgress))
        {
            personalProgress[goalIndex] = progress;
            photonView.RPC("SyncPersonalProgress", RpcTarget.All, characterName, goalIndex, progress);
        }
    }  

    [PunRPC]
    private void SyncPersonalProgress(string characterName, int goalIndex, float progress)
    {
        if (playerPersonalProgress.TryGetValue(characterName, out float[] personalProgress))
        {
            personalProgress[goalIndex] = progress;
            UpdatePlayerProfileUI(characterName);
        }
    }

    public void EndChallenge()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["PlayerScores"] = playerScores;
            PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
            PhotonNetwork.LoadLevel("ResultsScene");
        }
    }

    [PunRPC]
    private void SyncGameState(string challengeJson, float time, Dictionary<string, int> scores)
    {
        SerializableChallengeData serializableChallenge = JsonUtility.FromJson<SerializableChallengeData>(challengeJson);
        currentChallenge = ScriptableObject.CreateInstance<ChallengeData>();
        currentChallenge.title = serializableChallenge.title;
        currentChallenge.description = serializableChallenge.description;
        currentChallenge.milestones = serializableChallenge.milestones;
        currentChallenge.goalScore = serializableChallenge.goalScore;

        remainingTime = time;
        playerScores = new Dictionary<string, int>(scores);
        challengeText.text = currentChallenge.title;
        UpdateTimer(time);
        UpdateScoreDisplay();
        UpdateMilestonesDisplay();
    }

    private void UpdateScoreDisplay()
    {
        float[] milestoneProgress = new float[currentChallenge.milestones.Count];
        for (int i = 0; i < currentChallenge.milestones.Count; i++)
        {
            milestoneProgress[i] = milestoneCompletion[currentChallenge.milestones[i]] ? 1f : 0f;
        }
        challengeProgressUI.UpdateMilestoneProgress(milestoneProgress);

        PlayerProfileManager playerProfileManager = PlayerProfileManager.Instance;
        if (playerProfileManager != null)
        {
            foreach (var kvp in playerScores)
            {
                string characterName = kvp.Key;
                UpdatePlayerProfileUI(characterName);
            }
        }
    }

    private void UpdatePlayerProfileUI(string characterName)
    {
        if (playerPersonalProgress.TryGetValue(characterName, out float[] personalProgress))
        {
            PlayerProfileManager.Instance.UpdatePlayerProgress(characterName, playerScores[characterName], personalProgress);
        }
    }

    public void UpdatePlayerProgress(UniversalCharacterController character, float[] personalProgress)
    {
        character.UpdateProgress(personalProgress);
        PlayerProfileManager.Instance.UpdatePlayerProgress(character.characterName, playerScores[character.characterName], personalProgress);
    }

    public void UpdatePlayerEurekas(UniversalCharacterController character, int eurekaCount)
    {
        character.IncrementEurekaCount();
        PlayerProfileManager.Instance.UpdatePlayerEurekas(character.characterName, eurekaCount);
    }

    public int GetPlayerScore(string playerName)
    {
        if (playerScores.TryGetValue(playerName, out int score))
        {
            return score;
        }
        return 0;
    }

    public GameState GetCurrentGameState()
    {
        return new GameState(
            currentChallenge,
            CalculateCollectiveProgress(),
            new Dictionary<string, int>(playerScores),
            remainingTime,
            new Dictionary<string, bool>(milestoneCompletion)
        );
    }

    public int GetCollectiveProgress()
    {
        return CalculateCollectiveProgress();
    }

    private int CalculateCollectiveProgress()
    {
        return (int)((float)milestoneCompletion.Count(m => m.Value) / milestoneCompletion.Count * 100);
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

    public List<string> GetAICharacterNames()
    {
        return spawnedCharacters.Where(kvp => !kvp.Value.IsPlayerControlled).Select(kvp => kvp.Key).ToList();
    }

    public void ResetPlayerPositions()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_ResetPlayerPositions", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_ResetPlayerPositions()
    {
        foreach (var character in spawnedCharacters.Values)
        {
            if (characterLocations.TryGetValue(character.characterName, out string locationName))
            {
                Transform spawnPoint = GetSpawnPointByName(locationName);
                if (spawnPoint != null)
                {
                    character.ResetToSpawnPoint(spawnPoint.position);
                }
            }
        }
    }

    public Color GetCurrentHubColor()
    {
        return currentHub.hubColor;
    }

    public void UpdateMilestoneProgress(string characterName, string actionName)
    {
        float progressIncrement = 0.2f; // 20% progress per relevant action
        float[] milestoneProgress = new float[currentChallenge.milestones.Count];

        for (int i = 0; i < currentChallenge.milestones.Count; i++)
        {
            if (currentChallenge.milestones[i].ToLower().Contains(actionName.ToLower()))
            {
                milestoneProgress[i] = Mathf.Min(milestoneCompletion[currentChallenge.milestones[i]] ? 1f : milestoneProgress[i] + progressIncrement, 1f);
                if (milestoneProgress[i] >= 1f && !milestoneCompletion[currentChallenge.milestones[i]])
                {
                    CompleteMilestone(characterName, currentChallenge.milestones[i], i);
                }
            }
            else
            {
                milestoneProgress[i] = milestoneCompletion[currentChallenge.milestones[i]] ? 1f : 0f;
            }
        }

        challengeProgressUI.UpdateMilestoneProgress(milestoneProgress);
    }
}