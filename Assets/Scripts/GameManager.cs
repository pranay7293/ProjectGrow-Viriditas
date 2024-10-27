using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using static CharacterState;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float challengeDuration = 900f;
    [SerializeField] private float minimumPlayTime = 60f;

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

    [Header("Audio")]
    [SerializeField] private MusicManager musicManager;

    [Header("Tutorial System")]
    [SerializeField] private ChallengeSplashManager splashManager;
    [SerializeField] private TutorialManager tutorialManager;


    private float remainingTime;
    private float gameStartTime;
    private ChallengeData currentChallenge;
    private HubData currentHub;
    private Dictionary<string, bool> milestoneCompletion = new Dictionary<string, bool>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private Dictionary<string, Dictionary<string, float>> playerProgress = new Dictionary<string, Dictionary<string, float>>();
    private List<string> recentPlayerActions = new List<string>();
    private Dictionary<string, UniversalCharacterController> spawnedCharacters = new Dictionary<string, UniversalCharacterController>();
    private Dictionary<string, int> playerEurekas = new Dictionary<string, int>();
    private bool isEmergentScenarioActive = false;

    private readonly Dictionary<string, string> characterLocations = new Dictionary<string, string>
    {
        {"Aspen Rodriguez", "Biofoundry"},
        {"Astra Kim", "Space Center"},
        {"Celeste Dubois", "Gallery"},
        {"Dr. Cobalt Johnson", "Research Lab"},
        {"Dr. Eden Kapoor", "Medical Bay"},
        {"Dr. Flora Tremblay", "Biosecurity Hub"},
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
    if (!PhotonNetwork.IsMasterClient)
    {
        return;
    }

    if (milestonesDisplay != null)
    {
        milestonesDisplay.SetActive(false);
    }

    // Verify essential components
    if (splashManager == null)
    {
        Debug.LogWarning("ChallengeSplashManager reference missing in GameManager");
    }
    if (tutorialManager == null)
    {
        Debug.LogWarning("TutorialManager reference missing in GameManager");
    }

    StartCoroutine(GameStartSequence());
}

private System.Collections.IEnumerator GameStartSequence()
{
    // Initialize challenge and hub first
    currentChallenge = GetSelectedChallenge();
    currentHub = GetSelectedHub();
    
    if (currentChallenge == null || currentHub == null)
    {
        Debug.LogError("Failed to initialize challenge or hub. Cannot start game sequence.");
        yield break;
    }

    // Ensure challenge has its icon
    if (currentChallenge.iconSprite == null)
    {
        Debug.LogError($"Challenge {currentChallenge.title} is missing its icon sprite!");
    }

    // Show splash with proper challenge title and hub color
    if (ChallengeSplashManager.Instance != null)
    {
        ChallengeSplashManager.Instance.onSplashComplete += OnSplashComplete;
        ChallengeSplashManager.Instance.DisplayChallengeSplash(currentChallenge.title, currentHub.hubColor);
    }
    else
    {
        Debug.LogWarning("ChallengeSplashManager not found. Skipping splash screen.");
    }
    
    yield return new WaitForSeconds(4f);
    
    InitializeGame();
    
    if (TutorialManager.Instance != null)
    {
        TutorialManager.Instance.StartTutorial(currentChallenge, currentHub);
    }
    else
    {
        Debug.LogWarning("TutorialManager not found. Skipping tutorial.");
    }
}

    private void OnSplashComplete()
    {
        ChallengeSplashManager.Instance.onSplashComplete -= OnSplashComplete;
    }

    private bool hasPlayedFirstMusicCue = false;
    private bool hasPlayedSecondMusicCue = false;

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && !isEmergentScenarioActive)
        {
            UpdateGameTime();
            CheckForMusicCues(); // Added method call
            CheckForEmergentScenario();
            if (ShouldEndGame())
            {
                EndChallenge();
            }
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
            playerProgress[characterName] = new Dictionary<string, float>();
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
    
    // Define the correct order of hub names
    string[] correctOrder = new string[] 
    {
        "EcoHubData",
        "SpaceHubData",
        "HealthHubData",
        "FashionHubData",
        "FoodHubData",
        "DefenseHubData"
    };

    // Reorder the hubs array
    HubData[] orderedHubs = new HubData[correctOrder.Length];
    for (int i = 0; i < correctOrder.Length; i++)
    {
        orderedHubs[i] = allHubs.FirstOrDefault(h => h.name == correctOrder[i]);
        if (orderedHubs[i] == null)
        {
            Debug.LogError($"Could not find hub data for {correctOrder[i]}");
        }
    }

    if (selectedHubIndex >= 0 && selectedHubIndex < orderedHubs.Length)
    {
        HubData selectedHub = orderedHubs[selectedHubIndex];
        // Debug.Log($"Selected Hub: {selectedHub.hubName}, Color: {selectedHub.hubColor}, Index: {selectedHubIndex}");
        return selectedHub;
    }
    else
    {
        // Debug.LogError($"Selected hub index {selectedHubIndex} out of range. Using default hub.");
        return orderedHubs[0];
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
        
        // Debug.Log($"Attempting to get challenge: {challengeTitle}");
        
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

    public bool IsMilestonesDisplayVisible()
    {
        return milestonesDisplay != null && milestonesDisplay.activeSelf;
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
    // bool allMilestonesCompleted = milestoneCompletion.Count > 0 && milestoneCompletion.All(m => m.Value);
    bool minimumTimeMet = elapsedTime >= minimumPlayTime;

    // Return true only if time is up and minimum playtime is met
    return timeUp && minimumTimeMet;
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

    // private bool hasTriggered5MinScenario = false;
    // private bool hasTriggered10MinScenario = false;

//     public string GetCurrentActTitle()
// {
//     if (hasTriggered5MinScenario && !hasTriggered10MinScenario)
//     {
//         return "ACT II";
//     }
//     else if (hasTriggered10MinScenario)
//     {
//         return "ACT III";
//     }
//     return "";
// }

    private void CheckForMusicCues()
    {
        float gameTime = Time.time - gameStartTime;

        if (!hasPlayedFirstMusicCue && gameTime >= 275f && gameTime < 276f)
        {
            if (musicManager != null)
            {
                musicManager.PlayMusic();
                hasPlayedFirstMusicCue = true;
            }
        }
        else if (!hasPlayedSecondMusicCue && gameTime >= 575f && gameTime < 576f)
        {
            if (musicManager != null)
            {
                musicManager.PlayMusic();
                hasPlayedSecondMusicCue = true;
            }
        }
    }

   private void CheckForEmergentScenario()
{
    float gameTime = Time.time - gameStartTime;

    if (gameTime >= 300f && gameTime < 301f)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (musicManager != null)
            {
                musicManager.StopMusic();
            }
            TriggerEmergentScenario();
        }
    }
    else if (gameTime >= 600f && gameTime < 601f)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (musicManager != null)
            {
                musicManager.StopMusic();
            }
            TriggerEmergentScenario();
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

        isEmergentScenarioActive = true;
        GameState currentState = GetCurrentGameState();
        var scenarios = await scenarioGenerator.GenerateScenarios(currentState, GetRecentPlayerActions());
        if (scenarios != null && scenarios.Count > 0)
        {
            photonView.RPC("RPC_DisplayEmergentScenarios", RpcTarget.All, scenarios.Select(s => s.description).ToArray());
        }
        else
        {
            Debug.LogError("Failed to generate scenarios.");
            isEmergentScenarioActive = false;
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
            isEmergentScenarioActive = false;
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

    public void UpdateGameState(string characterName, string actionName, bool isEmergentScenario = false)
    {
        if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(actionName))
        {
            Debug.LogWarning("Invalid character name or action in UpdateGameState");
            return;
        }

        if (isEmergentScenario)
        {
            Debug.Log($"Emergent Scenario: {actionName}");
            ActionLogManager.Instance.LogAction("SYSTEM", $"Emergent Scenario: {actionName}");
        }
        else
        {
            AddPlayerAction(actionName);
            ActionLogManager.Instance.LogAction(characterName, actionName);

            UniversalCharacterController character = GetCharacterByName(characterName);
            if (character != null && character.currentAction != null)
            {
                int scoreChange = ScoreConstants.GetActionPoints(character.currentAction.duration);
                List<(string tag, float weight)> tagsWithWeights = TagSystem.GetTagsForAction(actionName);
                List<string> tags = tagsWithWeights.Select(t => t.tag).ToList();

                UpdatePlayerScore(characterName, scoreChange, actionName, tags);
                UpdateMilestoneProgress(characterName, actionName, tagsWithWeights);

                if (!character.IsPlayerControlled && !character.HasState(CharacterState.PerformingAction))
                {
                    AIManager aiManager = character.GetComponent<AIManager>();
                    if (aiManager != null)
                    {
                        aiManager.ConsiderCollaboration();
                    }
                }
            }
        }
    }

    public void ImplementEmergentScenario(string scenario)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_ImplementEmergentScenario", RpcTarget.All, scenario);
        }
    }

    [PunRPC]
    private void RPC_ImplementEmergentScenario(string scenario)
    {
        isEmergentScenarioActive = true;
        
        // Log the emergent scenario
        ActionLogManager.Instance.LogAction("SYSTEM", $"Emergent Scenario: {scenario}");

        // Update game state based on the scenario
        UpdateGameState("SYSTEM", scenario, true);

        // Display notification
        if (emergentScenarioNotification != null)
        {
            emergentScenarioNotification.DisplayNotification(scenario);
        }
        else
        {
            Debug.LogWarning("EmergentScenarioNotification is not assigned in GameManager");
        }
    }

    public void EndEmergentScenario()
    {
        isEmergentScenarioActive = false;
    }

    public void CompleteMilestone(string characterName, string milestone, int milestoneIndex)
    {
        if (milestoneCompletion[milestone] == false)
        {
            milestoneCompletion[milestone] = true;
            UpdatePlayerScore(characterName, ScoreConstants.KEY_MILESTONE_COMPLETION, "Milestone Completion", new List<string> { milestone });
            photonView.RPC("SyncMilestoneCompletion", RpcTarget.All, milestone, characterName, milestoneIndex);
            CheckAllMilestonesCompleted();
        }
    }

    [PunRPC]
    private void SyncMilestoneCompletion(string milestone, string characterName, int milestoneIndex)
    {
        milestoneCompletion[milestone] = true;
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
            // EndChallenge();
        }
    }

    public bool IsMilestoneCompleted(string milestone)
    {
        return milestoneCompletion.ContainsKey(milestone) && milestoneCompletion[milestone];
    }

    public void UpdatePlayerScore(string playerName, int score, string actionName, List<string> tags)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!playerScores.ContainsKey(playerName))
            {
                playerScores[playerName] = 0;
            }
            playerScores[playerName] += score;

            UpdateProgressBasedOnTags(playerName, tags);

            photonView.RPC("SyncPlayerScore", RpcTarget.All, playerName, playerScores[playerName], score, actionName);
        }
    }

    public void UpdateProgressBasedOnTags(string playerName, List<string> tags)
    {
        if (!playerProgress.ContainsKey(playerName))
        {
            playerProgress[playerName] = new Dictionary<string, float>();
        }

        foreach (var tag in tags)
        {
            if (!playerProgress[playerName].ContainsKey(tag))
            {
                playerProgress[playerName][tag] = 0f;
            }
            playerProgress[playerName][tag] = Mathf.Clamp01(playerProgress[playerName][tag] + ScoreConstants.PRIMARY_TAG_CONTRIBUTION);
        }

        UpdatePlayerProfileUI(playerName);
    }

    private void UpdateProgress(string playerName, string tag, int score)
{
    float progressIncrement = (float)score / ScoreConstants.KEY_MILESTONE_COMPLETION; // e.g., 30 / 1000 = 0.03
    
    if (currentChallenge.milestoneTags.Contains(tag))
    {
        int milestoneIndex = currentChallenge.milestoneTags.IndexOf(tag);
        float currentProgress = challengeProgressUI.GetMilestoneProgress(milestoneIndex);
        float newProgress = Mathf.Clamp01(currentProgress + progressIncrement);
        challengeProgressUI.UpdateMilestoneProgress(new float[] { newProgress });
        
        if (newProgress >= 1f && !milestoneCompletion[tag])
        {
            CompleteMilestone(playerName, tag, milestoneIndex);
        }
        }
        else
        {
            if (!playerProgress.ContainsKey(playerName))
            {
                playerProgress[playerName] = new Dictionary<string, float>();
            }
            if (!playerProgress[playerName].ContainsKey(tag))
            {
                playerProgress[playerName][tag] = 0f;
            }
            playerProgress[playerName][tag] = Mathf.Clamp01(playerProgress[playerName][tag] + progressIncrement);
        }

        UpdatePlayerProfileUI(playerName);
    }

    private void UpdatePlayerProfileUI(string characterName)
    {
        if (playerProgress.TryGetValue(characterName, out Dictionary<string, float> progress))
        {
            PlayerProfileManager.Instance.UpdatePlayerProfile(characterName, playerScores[characterName], progress);
        }
    }

    [PunRPC]
    private void SyncPlayerScore(string playerName, int totalScore, int scoreChange, string actionName)
    {
        playerScores[playerName] = totalScore;

        UniversalCharacterController character = GetCharacterByName(playerName);
        if (character != null)
        {
            // Update CharacterProgressBar
            character.UpdateProgress(playerProgress[playerName]);

            // Show floating text only for non-Eureka score changes
            if (actionName != "Eureka Moment")
            {
                Vector3 textPosition = character.transform.position + Vector3.up * 2f;
                string scoreText = "+" + scoreChange;
                FloatingTextManager.Instance.ShowFloatingText(scoreText, textPosition, FloatingTextType.ActionPoints);
            }
        }

        // Update PlayerProfileUI
        PlayerProfileManager.Instance.UpdatePlayerProfile(playerName, totalScore, playerProgress[playerName]);

        // Trigger a sort in PlayerProfileManager
        PlayerProfileManager.Instance.SortPlayersByScore();

        // Log the score change
        string logMessage = $"{playerName} gained {scoreChange} points. Action: {actionName}";
        ActionLogManager.Instance.LogAction("SYSTEM", logMessage);
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
        currentChallenge.milestones = new List<string>(serializableChallenge.milestones);
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

    public void UpdatePlayerEurekas(UniversalCharacterController character, int eurekaCount)
{
    // Remove the recursive call
    // character.IncrementEurekaCount();

    // Update the player's Eureka count in the GameManager's dictionary
    if (playerEurekas.ContainsKey(character.characterName))
    {
        playerEurekas[character.characterName] = eurekaCount;
    }
    else
    {
        playerEurekas.Add(character.characterName, eurekaCount);
    }

    // Update the Player Profile UI
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

    public void UpdateMilestoneProgress(string characterName, string actionName, List<(string tag, float weight)> tagsWithWeights)
{
    float[] milestoneProgress = new float[currentChallenge.milestones.Count];

    foreach (var (tag, weight) in tagsWithWeights)
    {
        if (currentChallenge.tagToSliderIndex.TryGetValue(tag, out int sliderIndex))
        {
            milestoneProgress[sliderIndex] += weight;
            if (milestoneProgress[sliderIndex] >= 1f && !milestoneCompletion[currentChallenge.milestones[sliderIndex]])
            {
                CompleteMilestone(characterName, currentChallenge.milestones[sliderIndex], sliderIndex);
            }
        }
    }

    challengeProgressUI.UpdateMilestoneProgress(milestoneProgress);
    UpdateScoreDisplay();
}
}
