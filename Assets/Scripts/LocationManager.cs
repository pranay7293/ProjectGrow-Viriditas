using UnityEngine;
using System.Collections.Generic;

public class LocationManager : MonoBehaviour
{
    private static Dictionary<string, Vector3> locationPositions = new Dictionary<string, Vector3>();

    public Color locationColor = Color.white;

    private void Awake()
    {
        ApplyColorToAllRenderers();
        locationPositions[gameObject.name] = transform.position;
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

    public static Vector3 GetLocationPosition(string locationName)
    {
        if (locationPositions.TryGetValue(locationName, out Vector3 position))
        {
            return position;
        }
        return Vector3.zero;
    }
}