using UnityEngine;
using Photon.Pun;

public class Karyo_GameCore : MonoBehaviourPunCallbacks
{
    public static Karyo_GameCore Instance { get; private set; }

    public UIManager uiManager;
    public GameManager gameManager;

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

    public override void OnJoinedRoom()
    {
        uiManager.UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        uiManager.UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        uiManager.UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount);
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Photon.Pun;

// [DefaultExecutionOrder(-1000)]
// public class Karyo_GameCore : MonoBehaviourPunCallbacks
// {
//     public static Karyo_GameCore Instance { get; private set; }

//     [Header("Subcomponent references")]
//     public WorldRep worldRep;
//     public InputManager inputManager;
//     public UIManager uiManager;
//     public KaryoUnityInputSource karyoUnityInputSource;
//     public TargetAcquisition targetAcquisition;
//     public PersistentData persistentData;
//     public SceneConfiguration sceneConfiguration;
//     public OpenAIService openAiService;

//     public float DEBUG_TimeScale = 1f;

//     private Dictionary<int, NPC_PlayerObjective> currentPlayerObjectives = new Dictionary<int, NPC_PlayerObjective>();
//     private Dictionary<int, List<NPC_PlayerObjective>> completedObjectives = new Dictionary<int, List<NPC_PlayerObjective>>();

//     private void Awake()
//     {
//         if (Instance != null)
//         {
//             Debug.LogError("Found multiple instances of Karyo_GameCore - this should not happen!");
//             Destroy(gameObject);
//             return;
//         }
//         Instance = this;

//         InitializeComponents();

//         if (DEBUG_TimeScale != 1f)
//         {
//             Debug.Log($"Setting time scale to {DEBUG_TimeScale}. Set it to 1.0 in GameCore to prevent this.");
//             Time.timeScale = DEBUG_TimeScale;
//         }
//     }

//     private void InitializeComponents()
//     {
//         worldRep = FindOrLogError<WorldRep>("WorldRep");
//         inputManager = FindOrLogError<InputManager>("InputManager");
//         uiManager = FindOrLogError<UIManager>("UIManager");
//         karyoUnityInputSource = FindOrLogError<KaryoUnityInputSource>("KaryoUnityInputSource");
//         targetAcquisition = FindOrLogError<TargetAcquisition>("TargetAcquisition");
//         persistentData = FindOrLogError<PersistentData>("PersistentData");
//         sceneConfiguration = FindOrLogError<SceneConfiguration>("SceneConfiguration");
//         openAiService = FindOrLogError<OpenAIService>("OpenAIService");
//     }

//     private T FindOrLogError<T>(string componentName) where T : Component
//     {
//         T component = FindObjectOfType<T>();
//         if (component == null)
//             Debug.LogError($"GameCore can't find {componentName}.");
//         return component;
//     }

//     private void OnDestroy()
//     {
//         Instance = null;
//     }

//     public void CreateObjective(NPC_PlayerObjective objective)
//     {
//         int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
//         if (DoesPlayerCurrentlyHaveAnObjective(localPlayerActorNumber))
//             Debug.LogWarning($"Player {localPlayerActorNumber} is being given an objective, but they already have an active objective which is {objective}");

//         photonView.RPC("RPC_CreateObjective", RpcTarget.All, localPlayerActorNumber, objective);
//     }

//     [PunRPC]
//     private void RPC_CreateObjective(int playerActorNumber, NPC_PlayerObjective objective)
//     {
//         currentPlayerObjectives[playerActorNumber] = objective;

//         if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
//             uiManager.DisplayObjectivePopupNotification(objective);
//     }

//     public bool DoesPlayerCurrentlyHaveAnObjective()
//     {
//         return DoesPlayerCurrentlyHaveAnObjective(PhotonNetwork.LocalPlayer.ActorNumber);
//     }

//     public bool DoesPlayerCurrentlyHaveAnObjective(int playerActorNumber)
//     {
//         return currentPlayerObjectives.ContainsKey(playerActorNumber);
//     }

//     public bool HasPlayerAlreadyFulfilledThisObjective(NPC_PlayerObjective objective)
//     {
//         int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
//         return completedObjectives.TryGetValue(localPlayerActorNumber, out var playerCompletedObjectives) && 
//                playerCompletedObjectives.Contains(objective);
//     }

//     public void PlayerHasFulfilledObjective()
//     {
//         int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
//         photonView.RPC("RPC_PlayerHasFulfilledObjective", RpcTarget.All, localPlayerActorNumber);
//     }

//     [PunRPC]
//     private void RPC_PlayerHasFulfilledObjective(int playerActorNumber)
//     {
//         if (!currentPlayerObjectives.TryGetValue(playerActorNumber, out NPC_PlayerObjective objective))
//         {
//             Debug.LogWarning($"PlayerHasFulfilledObjective() being called for player {playerActorNumber}, but they did not have a current objective.");
//             return;
//         }

//         string body_text = $"Great work! Player {playerActorNumber} finished the objective:\n" + objective.title;

//         if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
//             uiManager.LaunchGenericDialogWindow("Objective Complete", body_text, true);

//         if (!completedObjectives.ContainsKey(playerActorNumber))
//             completedObjectives[playerActorNumber] = new List<NPC_PlayerObjective>();

//         completedObjectives[playerActorNumber].Add(objective);
//         currentPlayerObjectives.Remove(playerActorNumber);
//     }

//     public string CompletedObjectivesAsString()
//     {
//         int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
//         if (!completedObjectives.TryGetValue(localPlayerActorNumber, out var playerCompletedObjectives))
//             return "";

//         return string.Join("\n", playerCompletedObjectives.ConvertAll(obj => obj.title));
//     }

//     public NPC_PlayerObjective GetCurrentPlayerObjective()
//     {
//         int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
//         if (currentPlayerObjectives.TryGetValue(localPlayerActorNumber, out NPC_PlayerObjective objective))
//         {
//             return objective;
//         }
//         return null;
//     }

//     public static void Shuffle<T>(List<T> list)
//     {
//         if (list.Count <= 1) return;

//         for (int i = 0; i < list.Count - 1; i++)
//         {
//             int j = Random.Range(i, list.Count);
//             T tmp = list[i];
//             list[i] = list[j];
//             list[j] = tmp;
//         }
//     }

//     public static void Shuffle<T>(T[] array)
//     {
//         if (array.Length <= 1) return;

//         for (int i = 0; i < array.Length - 1; i++)
//         {
//             int j = Random.Range(i, array.Length);
//             T tmp = array[i];
//             array[i] = array[j];
//             array[j] = tmp;
//         }
//     }

//     public UniversalCharacterController GetLocalPlayerCharacter()
//     {
//         UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
//         return System.Array.Find(characters, c => c.photonView.IsMine && c.IsPlayerControlled);
//     }
// }