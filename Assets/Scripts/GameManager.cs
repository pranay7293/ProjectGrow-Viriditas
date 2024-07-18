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

    private int challengeGoal = 1000; // Example goal, adjust as needed

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
        UpdateGameTime();
        CheckForNewScenario();
    }

    private void InitializeGame()
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
        if (PhotonNetwork.IsMasterClient)
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
    }

    private void CheckForNewScenario()
    {
        if (PhotonNetwork.IsMasterClient && Time.time - lastScenarioTime >= scenarioGenerationInterval)
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
        // TODO: Implement logic to fade in/out or animate the scenario display
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
        UpdateCollectiveScore(EvaluateActionImpact(action));
        AddPlayerAction(action);
    }

    private int EvaluateActionImpact(string action)
    {
        // TODO: Implement more sophisticated action impact evaluation
        return Random.Range(1, 10);
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
    private void UpdateTimer(float time)
    {
        remainingTime = time;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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
        collectiveScoreDisplay.text = "Collective Score: " + collectiveScore;
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

// using UnityEngine;
// using Photon.Pun;
// using System.Collections.Generic;

// public class GameManager : MonoBehaviourPunCallbacks
// {
//     public string characterPrefabName = "Character";
//     public NPC_Data npcData;
//     private Dictionary<int, UniversalCharacterController> spawnedCharacters = new Dictionary<int, UniversalCharacterController>();

//     [SerializeField] private GameObject cameraRigPrefab;

//     private void Start()
//     {
//         if (PhotonNetwork.IsMasterClient)
//         {
//             SpawnCharacters();
//         }
//     }

//     private void SpawnCharacters()
//     {
//         if (npcData == null || npcData.characters == null)
//         {
//             Debug.LogError("NPC_Data or characters are missing.");
//             return;
//         }

//         List<string> availableCharacters = npcData.GetAllCharacterNames();
        
//         // Spawn player-controlled characters
//         foreach (var player in PhotonNetwork.PlayerList)
//         {
//             if (player.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
//             {
//                 string characterName = (string)selectedCharacter;
//                 availableCharacters.Remove(characterName);
//                 SpawnCharacter(characterName, true, player.ActorNumber - 1);
//             }
//         }

//         // Spawn AI-controlled characters
//         for (int i = 0; i < availableCharacters.Count; i++)
//         {
//             SpawnCharacter(availableCharacters[i], false, PhotonNetwork.PlayerList.Length + i);
//         }
//     }

//     private void SpawnCharacter(string characterName, bool isPlayerControlled, int spawnIndex)
//     {
//         NPC_Data.CharacterData characterData = npcData.GetCharacterData(characterName);
//         if (characterData == null)
//         {
//             Debug.LogError($"Character data not found for {characterName}");
//             return;
//         }

//         if (characterData.spawnLocation == null)
//         {
//             Debug.LogError($"Spawn location not set for character {characterName}");
//             return;
//         }

//         Vector3 spawnPosition = characterData.spawnLocation.transform.position;
//         Quaternion spawnRotation = characterData.spawnLocation.transform.rotation;

//         object[] instantiationData = new object[] 
//         { 
//             characterName,
//             isPlayerControlled,
//             characterData.specificCharacterPrompt_part2,
//             characterData.customActionTextDescription,
//             // characterData.playerObjectives,
//             npcData.genericPrompt_part1,
//             npcData.requestPrompt_part5,
//             npcData.requestPrompt_part5_w_objectives,
//             npcData.requestPrompt_part5_dialogOptions,
//             npcData.objectiveInclusionPercentChance,
//             npcData.objectiveExclusionDuration,
//             npcData.requiredConversationDepth,
//             npcData.distanceToUseRandomActions,
//             npcData.duration_SecPerWord,
//             npcData.minDialogDuration,
//             npcData.maxDialogDuration,
//             npcData.idleDurationMin,
//             npcData.idleDurationMax,
//             npcData.nearbyThreshold
//         };

//         string prefabName = $"Character-{characterName}";
//         GameObject characterGO = PhotonNetwork.Instantiate(prefabName, spawnPosition, spawnRotation, 0, instantiationData);
//         if (characterGO == null)
//         {
//             Debug.LogError($"Failed to instantiate character {characterName}");
//             return;
//         }

//         UniversalCharacterController character = characterGO.GetComponent<UniversalCharacterController>();
//         if (character != null)
//         {
//             character.photonView.RPC("Initialize", RpcTarget.All, characterName, isPlayerControlled);
//             spawnedCharacters[spawnIndex] = character;

//             if (isPlayerControlled && character.photonView.IsMine)
//             {
//                 SetupPlayerCamera(character);
//             }
//         }
//         else
//         {
//             Debug.LogError($"UniversalCharacterController component not found on instantiated character {characterName}");
//         }
//     }

//     private void SetupPlayerCamera(UniversalCharacterController character)
//     {
//         GameObject cameraRig = Instantiate(cameraRigPrefab, character.transform.position, Quaternion.identity);
//         com.ootii.Cameras.CameraController cameraController = cameraRig.GetComponent<com.ootii.Cameras.CameraController>();
//         if (cameraController != null)
//         {
//             cameraController.Anchor = character.transform;
//         }
//         else
//         {
//             Debug.LogError("CameraController component not found on CameraRig prefab");
//         }

//         AudioListener audioListener = cameraRig.GetComponentInChildren<AudioListener>();
//         if (audioListener != null)
//         {
//             audioListener.enabled = true;
//         }

//         AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
//         foreach (AudioListener listener in allListeners)
//         {
//             if (listener != audioListener)
//             {
//                 listener.enabled = false;
//             }
//         }
//     }

//     public UniversalCharacterController GetCharacterByIndex(int index)
//     {
//         if (spawnedCharacters.TryGetValue(index, out UniversalCharacterController character))
//         {
//             return character;
//         }
//         return null;
//     }

//     public void SwitchCharacterControl(int characterIndex, bool toPlayerControl)
//     {
//         if (spawnedCharacters.TryGetValue(characterIndex, out UniversalCharacterController character))
//         {
//             character.SwitchControlMode(toPlayerControl);
//         }
//     }
// }