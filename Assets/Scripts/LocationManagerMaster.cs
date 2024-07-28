using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LocationManagerMaster : MonoBehaviour
{
    public static LocationManagerMaster Instance { get; private set; }

    private Dictionary<string, LocationManager> locations = new Dictionary<string, LocationManager>();

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

    public void RegisterLocation(LocationManager location)
    {
        if (!locations.ContainsKey(location.gameObject.name))
        {
            locations[location.gameObject.name] = location;
        }
    }

    public Vector3 GetLocationPosition(string locationName)
    {
        if (locations.TryGetValue(locationName, out LocationManager location))
        {
            return location.transform.position;
        }
        return Vector3.zero;
    }

    public List<string> GetLocationActions(string locationName)
    {
        if (locations.TryGetValue(locationName, out LocationManager location))
        {
            return location.GetActions();
        }
        return new List<string>();
    }

    public List<string> GetAllLocations()
    {
        return new List<string>(locations.Keys);
    }

    public string GetClosestLocation(Vector3 position)
    {
        return locations.OrderBy(kvp => Vector3.Distance(kvp.Value.transform.position, position)).First().Key;
    }

    public string GetTargetLocation(List<string> subgoals)
    {
        List<string> relevantLocations = new List<string>();

        foreach (var location in locations)
        {
            List<string> locationActions = location.Value.GetActions();
            if (locationActions.Any(action => subgoals.Any(subgoal => action.ToLower().Contains(subgoal.ToLower()))))
            {
                relevantLocations.Add(location.Key);
            }
        }

        if (relevantLocations.Count > 0)
        {
            return relevantLocations[Random.Range(0, relevantLocations.Count)];
        }

        return GetAllLocations()[Random.Range(0, GetAllLocations().Count)];
    }
}