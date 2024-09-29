using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WaypointsManager : MonoBehaviour
{
    public static WaypointsManager Instance { get; private set; }

    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float waypointRadius = 1f;
    [SerializeField] private float interactPointRadius = 2f;

    private Dictionary<string, Transform> locationInteractPoints = new Dictionary<string, Transform>();

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

        InitializeInteractPoints();
    }

    private void InitializeInteractPoints()
    {
        GameObject[] locations = GameObject.FindGameObjectsWithTag("Location");
        foreach (GameObject location in locations)
        {
            Transform interactPoint = location.transform.Find("InteractPoint");
            if (interactPoint != null)
            {
                locationInteractPoints[location.name] = interactPoint;
            }
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
        if (locationInteractPoints.TryGetValue(locationName, out Transform interactPoint))
        {
            return interactPoint.position;
        }

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

    public bool IsNearInteractPoint(Vector3 position, string locationName)
    {
        if (locationInteractPoints.TryGetValue(locationName, out Transform interactPoint))
        {
            return Vector3.Distance(position, interactPoint.position) <= interactPointRadius;
        }
        return false;
    }

    public Vector3 GetInteractPointPosition(string locationName)
    {
        if (locationInteractPoints.TryGetValue(locationName, out Transform interactPoint))
        {
            return interactPoint.position;
        }
        return Vector3.zero;
    }

    public List<Vector3> GetCollaborationPositions(string locationName, int collaboratorCount)
    {
        if (locationInteractPoints.TryGetValue(locationName, out Transform interactPoint))
        {
            List<Vector3> positions = new List<Vector3>();
            float angleStep = 360f / collaboratorCount;
            for (int i = 0; i < collaboratorCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * interactPointRadius;
                positions.Add(interactPoint.position + offset);
            }
            return positions;
        }
        return null;
    }
}