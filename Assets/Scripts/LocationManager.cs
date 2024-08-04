using UnityEngine;
using System.Collections.Generic;

public class LocationManager : MonoBehaviour
{
    [System.Serializable]
    public class LocationAction
    {
        public string actionName;
        [TextArea(3, 5)]
        public string description;
        public float baseSuccessRate;
        public float duration;
        public string requiredRole; // Can be empty if no specific role is required
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
}