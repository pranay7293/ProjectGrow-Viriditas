using UnityEngine;
using System.Collections;
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

    private HashSet<UniversalCharacterController> charactersInLocation = new HashSet<UniversalCharacterController>();

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
        if (character != null && !charactersInLocation.Contains(character))
        {
            charactersInLocation.Add(character);
            character.EnterLocation(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UniversalCharacterController character = other.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            charactersInLocation.Remove(character);
            character.ExitLocation();
        }
    }

    public bool IsCharacterInLocation(UniversalCharacterController character)
    {
        return charactersInLocation.Contains(character);
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
        if (PhotonNetwork.IsMasterClient)
        {
            // Debug.Log($"Attempting to instantiate EurekaEffect at {transform.position}");
            GameObject eurekaEffect = PhotonNetwork.Instantiate("EurekaEffect", transform.position, Quaternion.identity);
            if (eurekaEffect == null)
            {
                Debug.LogError("Failed to instantiate EurekaEffect");
            }
            else
            {
                Debug.Log("EurekaEffect instantiated successfully");
                EurekaEffectController effectController = eurekaEffect.GetComponent<EurekaEffectController>();
                if (effectController != null)
                {
                    effectController.Initialize(locationColor);
                }
                else
                {
                    Debug.LogError("EurekaEffectController component not found on instantiated object");
                }
            }
        }
    }

    private IEnumerator DestroyEurekaEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeEurekaEffect != null)
        {
            Destroy(activeEurekaEffect);
        }
    }
}