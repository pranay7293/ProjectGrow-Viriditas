using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
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

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnCharacters();
        }
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
        }
        else
        {
            Debug.LogError($"UniversalCharacterController component not found on spawned character {characterName}");
        }
    }

    private Transform GetSpawnPointByName(string locationName)
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint.name == locationName)
            {
                return spawnPoint;
            }
        }
        return null;
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