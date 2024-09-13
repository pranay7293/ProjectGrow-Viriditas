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
        public int duration;
        public string requiredRole;
        public Sprite actionIcon;
        public List<string> tags = new List<string>();
    }

    public string locationName;
    public List<LocationAction> availableActions = new List<LocationAction>();
    public Color locationColor = new Color(1f, 1f, 1f, 1f);

    [SerializeField] private GameObject eurekaEffectPrefab;
    private GameObject activeEurekaEffect;

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

    public void UpdateCharacterAvailableActions(UniversalCharacterController character)
    {
        List<LocationAction> actions = GetAvailableActions(character.aiSettings.characterRole);
        character.UpdateAvailableActions(actions);
    }

    public List<LocationAction> GetAvailableActions(string characterRole)
    {
        return availableActions.FindAll(action => string.IsNullOrEmpty(action.requiredRole) || action.requiredRole == characterRole);
    }

    public LocationAction GetActionByName(string actionName)
    {
        return availableActions.Find(a => a.actionName == actionName);
    }

    [PunRPC]
    public void StartAction(string actionName, int characterViewID)
    {
        LocationAction action = GetActionByName(actionName);
        if (action == null) return;

        PhotonView characterView = PhotonView.Find(characterViewID);
        if (characterView == null) return;

        UniversalCharacterController character = characterView.GetComponent<UniversalCharacterController>();
        if (character == null) return;

        character.StartAction(action);
        
        GameManager.Instance.UpdatePlayerScore(character.characterName, ScoreConstants.GetActionPoints(action.duration), action.actionName, action.tags);
    }

    public void PlayEurekaEffect()
    {
        if (activeEurekaEffect != null)
        {
            Destroy(activeEurekaEffect);
        }

        Renderer locationRenderer = GetComponent<Renderer>();
        Vector3 center = locationRenderer != null 
            ? locationRenderer.bounds.center 
            : transform.position;

        center += Vector3.up * 1f;

        activeEurekaEffect = Instantiate(eurekaEffectPrefab, center, Quaternion.identity);
        Destroy(activeEurekaEffect, 5f);
    }
}