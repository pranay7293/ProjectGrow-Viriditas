using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float challengeDuration = 900f; // 15 minutes

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private GameObject milestonesDisplay;
    [SerializeField] private TextMeshProUGUI[] milestoneTexts;
    [SerializeField] private CustomCheckbox[] milestoneCheckboxes;
    [SerializeField] private ChallengeProgressUI challengeProgressUI;

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
    private int collectiveScore = 0;
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
        }

        // Handle input for toggling milestones display
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
        collectiveScore = 0;
        
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
        photonView.RPC("SyncGameState", RpcTarget.All, challengeJson, remainingTime, collectiveScore, playerScores);
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
            return allHubs[0]; // Return the first hub as a default
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

        var scenario = await scenarioGenerator.GenerateScenario(currentChallenge.title, recentPlayerActions);
        if (scenario != null)
        {
            photonView.RPC("RPC_DisplayEmergentScenario", RpcTarget.All, scenario.description, scenario.options.ToArray());
        }
        else
        {
            Debug.LogError("Failed to generate scenario.");
        }
    }

    [PunRPC]
    private void RPC_DisplayEmergentScenario(string description, string[] options)
    {
        Debug.Log($"RPC_DisplayEmergentScenario called at {Time.time}");

        if (scenarioUI == null)
        {
            scenarioUI = FindObjectOfType<EmergentScenarioUI>(true);
        }

        if (scenarioUI != null)
        {
            scenarioUI.DisplayScenario(new EmergentScenarioGenerator.ScenarioData
            {
                description = description,
                options = new List<string>(options)
            });
        }
        else
        {
            Debug.LogError("EmergentScenarioUI not found. Cannot display scenario.");
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

    public void UpdateGameState(string characterName, string action)
    {
        Debug.Log($"{characterName} performed action: {action}");
        int scoreIncrease = EvaluateActionImpact(action);
        UpdateCollectiveScore(scoreIncrease);
        AddPlayerAction(action);
        ActionLogManager.Instance.LogAction(characterName, action);

        if (ActionContributesToChallenge(action))
        {
            UpdateCollectiveScore(10);
        }

        CheckMilestoneProgress(characterName, action);
        UpdatePlayerScore(characterName, scoreIncrease);

        // Check for collaboration
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

    public void HandleCollabCompletion(string actionName, List<UniversalCharacterController> collaborators)
    {
        foreach (var collaborator in collaborators)
        {
            // Update scores and generate eurekas
            UpdatePlayerScore(collaborator.characterName, 10); // Base score for collaboration
            
            if (Random.value < 0.3f) // 30% chance to generate an eureka
            {
                int eurekaCount = collaborator.EurekaCount + 1;
                collaborator.UpdateEurekas(eurekaCount);
            }
        }

        // End the collaboration
        foreach (var collaborator in collaborators)
        {
            collaborator.EndCollab(actionName);
        }
    }

    private int EvaluateActionImpact(string action)
    {
        return Random.Range(1, 10);
    }

    private bool ActionContributesToChallenge(string action)
    {
        return action.ToLower().Contains(currentChallenge.title.ToLower());
    }

    private void CheckMilestoneProgress(string characterName, string action)
    {
        for (int i = 0; i < currentChallenge.milestones.Count; i++)
        {
            string milestone = currentChallenge.milestones[i];
            if (!milestoneCompletion[milestone] && action.ToLower().Contains(milestone.ToLower()))
            {
                CompleteMilestone(characterName, milestone, i);
                break;
            }
        }
    }

    public void CompleteMilestone(string characterName, string milestone, int milestoneIndex)
    {
        if (milestoneCompletion[milestone] == false)
        {
            milestoneCompletion[milestone] = true;
            UpdateCollectiveScore(100);
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
    }

    public void GenerateEureka(string player1, string player2)
    {
        playerEurekas[player1]++;
        playerEurekas[player2]++;
        UpdatePlayerScore(player1, 10);
        UpdatePlayerScore(player2, 10);
    }

    public void UseEureka(string playerName)
    {
        if (playerEurekas[playerName] > 0)
        {
            playerEurekas[playerName]--;
            UpdatePlayerScore(playerName, 20);
        }
    }

    private void CheckAllMilestonesCompleted()
    {
        if (milestoneCompletion.All(m => m.Value))
        {
            UpdateCollectiveScore(500);
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

    private void EndChallenge()
    {
        photonView.RPC("ShowResults", RpcTarget.All);
    }

    [PunRPC]
    private void SyncGameState(string challengeJson, float time, int collective, Dictionary<string, int> scores)
    {
        SerializableChallengeData serializableChallenge = JsonUtility.FromJson<SerializableChallengeData>(challengeJson);
        currentChallenge = ScriptableObject.CreateInstance<ChallengeData>();
        currentChallenge.title = serializableChallenge.title;
        currentChallenge.description = serializableChallenge.description;
        currentChallenge.milestones = serializableChallenge.milestones;
        currentChallenge.goalScore = serializableChallenge.goalScore;

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
            PlayerProfileManager.Instance.UpdatePlayerProgress(characterName, 0f, personalProgress);
        }
    }

    public void UpdatePlayerProgress(UniversalCharacterController character, float overallProgress, float[] personalProgress)
    {
        character.UpdateProgress(overallProgress, personalProgress);
        PlayerProfileManager.Instance.UpdatePlayerProgress(character.characterName, overallProgress, personalProgress);
    }

    public void UpdatePlayerEurekas(UniversalCharacterController character, int eurekaCount)
    {
        character.UpdateEurekas(eurekaCount);
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
}