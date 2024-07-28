using UnityEngine;
using System.Collections.Generic;

public class LocationManager : MonoBehaviour
{
    [SerializeField] private List<string> actions = new List<string>();
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
            Debug.Log($"{character.characterName} entered {gameObject.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UniversalCharacterController character = other.GetComponent<UniversalCharacterController>();
        if (character != null)
        {
            Debug.Log($"{character.characterName} exited {gameObject.name}");
        }
    }

    public List<string> GetActions()
    {
        return new List<string>(actions);
    }
}