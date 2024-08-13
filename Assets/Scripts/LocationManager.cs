using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class LocationManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class LocationAction
    {
        public string actionName;
        [TextArea(3, 5)]
        public string description;
        public float baseSuccessRate;
        public int duration; // 15, 30, or 45 seconds
        public string requiredRole;
        public Sprite actionIcon;
    }

    public string locationName;
    public List<LocationAction> availableActions = new List<LocationAction>();
    public Color locationColor = Color.white;

    private void Start()
    {
        ApplyColorToAllRenderers();
        LocationManagerMaster.Instance.RegisterLocation(this);
    }

    private void ApplyColorToAllRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = locationColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        UniversalCharacterController character = other.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            character.EnterLocation(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UniversalCharacterController character = other.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            character.ExitLocation();
        }
    }

    public List<LocationAction> GetAvailableActions(string characterRole)
    {
        return availableActions.FindAll(action => string.IsNullOrEmpty(action.requiredRole) || action.requiredRole == characterRole);
    }

    [PunRPC]
    public void StartAction(string actionName, int characterViewID)
    {
    LocationAction action = availableActions.Find(a => a.actionName == actionName);
    if (action == null) return;

    PhotonView characterView = PhotonView.Find(characterViewID);
    if (characterView == null) return;

    UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
    if (character == null) return;

    character.StartAction(action);
    }
}