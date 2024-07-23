using UnityEngine;
using System.Collections.Generic;

public class LocationManager : MonoBehaviour
{
    private static Dictionary<string, Vector3> locationPositions = new Dictionary<string, Vector3>();
    private static Dictionary<string, List<string>> locationActions = new Dictionary<string, List<string>>
    {
        {"Think Tank", new List<string> {"Debating", "Planning", "Reflecting"}},
        {"Innovation Hub", new List<string> {"Brainstorming", "Prototyping", "Fundraising"}},
        {"Medical Bay", new List<string> {"Treating Patients", "Researching", "Consulting"}},
        {"Maker Space", new List<string> {"Tinkering", "Building", "Collaborating"}},
        {"Research Lab", new List<string> {"Experimenting", "Analyzing", "Documenting"}},
        {"Biofoundry", new List<string> {"Engineering", "Cultivating", "Automating"}},
        {"Sound Studio", new List<string> {"Composing", "Recording", "Experimenting"}},
        {"Gallery", new List<string> {"Creating", "Exhibiting", "Critiquing"}},
        {"Space Center", new List<string> {"Exploring", "Simulating", "Innovating"}},
        {"Media Center", new List<string> {"Writing", "Interviewing", "Editing"}}
    };

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
            TriggerLocationEvent(character);
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

    private void TriggerLocationEvent(UniversalCharacterController character)
    {
        if (locationActions.TryGetValue(gameObject.name, out List<string> actions))
        {
            string randomAction = actions[Random.Range(0, actions.Count)];
            character.PerformAction(randomAction);
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

    public static List<string> GetLocationActions(string locationName)
    {
        if (locationActions.TryGetValue(locationName, out List<string> actions))
        {
            return new List<string>(actions);
        }
        return new List<string>();
    }

    public static List<string> GetAllLocations()
    {
        return new List<string>(locationActions.Keys);
    }
}