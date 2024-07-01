using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[index % spawnPoints.Length];
        }
        Debug.LogWarning("No spawn points available.");
        return null;
    }
}