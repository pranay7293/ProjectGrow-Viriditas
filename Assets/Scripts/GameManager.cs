using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public string characterPrefabName = "Character";
    public NPC_Data npcData;
    private PlayerSpawnManager spawnManager;
    private Dictionary<int, UniversalCharacterController> spawnedCharacters = new Dictionary<int, UniversalCharacterController>();

    [SerializeField] private GameObject cameraRigPrefab;

    private void Awake()
    {
        spawnManager = GetComponent<PlayerSpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("PlayerSpawnManager not found on GameManager object.");
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnCharacters();
        }
    }

    private void SpawnCharacters()
    {
        if (npcData == null || npcData.characterNames == null)
        {
            Debug.LogError("NPC_Data or character names are missing.");
            return;
        }

        List<string> availableCharacters = new List<string>(npcData.characterNames);
        
        // Spawn player-controlled characters
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
            {
                string characterName = (string)selectedCharacter;
                availableCharacters.Remove(characterName);
                SpawnCharacter(characterName, true, player.ActorNumber - 1);
            }
        }

        // Spawn AI-controlled characters
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            SpawnCharacter(availableCharacters[i], false, PhotonNetwork.PlayerList.Length + i);
        }
    }

    private void SpawnCharacter(string characterName, bool isPlayerControlled, int spawnIndex)
{
    Transform spawnPoint = spawnManager.GetSpawnPoint(spawnIndex);
    if (spawnPoint == null)
    {
        Debug.LogError($"No spawn point available for character {characterName}");
        return;
    }

    // Pass only essential data
    object[] instantiationData = new object[] 
    { 
        characterName,
        isPlayerControlled,
        npcData.genericPrompt_part1,
        npcData.requestPrompt_part5,
        npcData.requestPrompt_part5_w_objectives,
        npcData.requestPrompt_part5_dialogOptions,
        npcData.objectiveInclusionPercentChance,
        npcData.objectiveExclusionDuration,
        npcData.requiredConversationDepth,
        npcData.distanceToUseRandomActions,
        npcData.duration_SecPerWord,
        npcData.minDialogDuration,
        npcData.maxDialogDuration,
        npcData.idleDurationMin,
        npcData.idleDurationMax,
        npcData.nearbyThreshold
    };
        GameObject characterGO = PhotonNetwork.Instantiate(characterPrefabName, spawnPoint.position, spawnPoint.rotation, 0, instantiationData);
        if (characterGO == null)
        {
            Debug.LogError($"Failed to instantiate character {characterName}");
            return;
        }

        UniversalCharacterController character = characterGO.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            character.photonView.RPC("Initialize", RpcTarget.All, characterName, isPlayerControlled);
            spawnedCharacters[spawnIndex] = character;

            if (isPlayerControlled && character.photonView.IsMine)
            {
                SetupPlayerCamera(character);
            }
        }
        else
        {
            Debug.LogError($"UniversalCharacterController component not found on instantiated character {characterName}");
        }
    }

    private void SetupPlayerCamera(UniversalCharacterController character)
    {
        GameObject cameraRig = Instantiate(cameraRigPrefab, character.transform.position, Quaternion.identity);
        com.ootii.Cameras.CameraController cameraController = cameraRig.GetComponent<com.ootii.Cameras.CameraController>();
        if (cameraController != null)
        {
            cameraController.Anchor = character.transform;
        }
        else
        {
            Debug.LogError("CameraController component not found on CameraRig prefab");
        }

        // Disable AudioListener on all but the local player's camera
        AudioListener audioListener = cameraRig.GetComponentInChildren<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = true;
        }

        // Disable all other AudioListeners in the scene
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
        foreach (AudioListener listener in allListeners)
        {
            if (listener != audioListener)
            {
                listener.enabled = false;
            }
        }
    }

    public UniversalCharacterController GetCharacterByIndex(int index)
    {
        if (spawnedCharacters.TryGetValue(index, out UniversalCharacterController character))
        {
            return character;
        }
        return null;
    }
}