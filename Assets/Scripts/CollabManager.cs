using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class CollabManager : MonoBehaviourPunCallbacks
{
    public static CollabManager Instance { get; private set; }

    private Dictionary<string, List<UniversalCharacterController>> activeCollaborations = new Dictionary<string, List<UniversalCharacterController>>();

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

    public bool CanCollaborate(LocationManager.LocationAction action)
    {
        return !activeCollaborations.ContainsKey(action.actionName) || activeCollaborations[action.actionName].Count < 3;
    }

    public void StartCollaboration(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        if (!activeCollaborations.ContainsKey(action.actionName))
        {
            activeCollaborations[action.actionName] = new List<UniversalCharacterController>();
        }

        if (!activeCollaborations[action.actionName].Contains(character))
        {
            photonView.RPC("RPC_AddCollaborator", RpcTarget.All, character.photonView.ViewID, action.actionName);
        }
    }

    [PunRPC]
    private void RPC_AddCollaborator(int characterViewID, string actionName)
    {
        PhotonView characterView = PhotonView.Find(characterViewID);
        if (characterView == null) return;

        UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
        if (character == null) return;

        if (!activeCollaborations.ContainsKey(actionName))
        {
            activeCollaborations[actionName] = new List<UniversalCharacterController>();
        }

        activeCollaborations[actionName].Add(character);
    }

    public void EndCollaboration(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        if (activeCollaborations.ContainsKey(action.actionName) && activeCollaborations[action.actionName].Contains(character))
        {
            photonView.RPC("RPC_RemoveCollaborator", RpcTarget.All, character.photonView.ViewID, action.actionName);
        }
    }

    [PunRPC]
    private void RPC_RemoveCollaborator(int characterViewID, string actionName)
    {
        PhotonView characterView = PhotonView.Find(characterViewID);
        if (characterView == null) return;

        UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
        if (character == null) return;

        if (activeCollaborations.ContainsKey(actionName))
        {
            activeCollaborations[actionName].Remove(character);
            if (activeCollaborations[actionName].Count == 0)
            {
                activeCollaborations.Remove(actionName);
            }
        }
    }

    public float GetCollaborationBonus(LocationManager.LocationAction action)
    {
        if (activeCollaborations.ContainsKey(action.actionName))
        {
            int collaboratorCount = activeCollaborations[action.actionName].Count;
            return collaboratorCount * 0.1f; // 10% bonus per collaborator
        }
        return 0f;
    }
}