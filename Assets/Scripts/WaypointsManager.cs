using UnityEngine;
using System.Collections.Generic;

public class WaypointsManager : MonoBehaviour
{
    public static WaypointsManager Instance { get; private set; }

    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float waypointRadius = 1f;

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

    public Vector3 GetRandomWaypoint()
    {
        if (waypoints.Count == 0) return Vector3.zero;
        return waypoints[Random.Range(0, waypoints.Count)].position;
    }

    public Vector3 GetNearestWaypoint(Vector3 position)
    {
        if (waypoints.Count == 0) return Vector3.zero;
        
        Transform nearestWaypoint = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform waypoint in waypoints)
        {
            float distance = Vector3.Distance(position, waypoint.position);
            if (distance < nearestDistance)
            {
                nearestWaypoint = waypoint;
                nearestDistance = distance;
            }
        }

        return nearestWaypoint != null ? nearestWaypoint.position : Vector3.zero;
    }

    public Vector3 GetWaypointNearLocation(string locationName)
    {
        Vector3 locationPosition = LocationManagerMaster.Instance.GetLocationPosition(locationName);
        return GetNearestWaypoint(locationPosition);
    }

    public bool IsNearWaypoint(Vector3 position)
    {
        foreach (Transform waypoint in waypoints)
        {
            if (Vector3.Distance(position, waypoint.position) <= waypointRadius)
            {
                return true;
            }
        }
        return false;
    }
}