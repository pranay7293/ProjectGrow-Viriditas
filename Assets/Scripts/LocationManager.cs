using UnityEngine;

public class LocationManager : MonoBehaviour
{
    public Color locationColor = Color.white;

    private void Awake()
    {
        ApplyColorToAllRenderers();
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
}