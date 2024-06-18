using UnityEngine;
using Photon.Pun;

public class PlayerSpawnManager : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex");
        string selectedCharacterPrefab = GetCharacterPrefabName(selectedCharacterIndex);

        if (!string.IsNullOrEmpty(selectedCharacterPrefab))
        {
            Transform spawnPoint = GetSpawnPoint(selectedCharacterIndex);
            PhotonNetwork.Instantiate(selectedCharacterPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    string GetCharacterPrefabName(int index)
    {
        switch (index)
        {
            case 0: return "Indigo";
            case 1: return "Astra";
            case 2: return "DrCobalt";
            case 3: return "Aspen";
            case 4: return "DrEden";
            case 5: return "Celeste";
            case 6: return "Sierra";
            case 7: return "Lilith";
            case 8: return "River";
            case 9: return "DrFlora";
            default: return null;
        }
    }

    Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > index)
        {
            return spawnPoints[index];
        }
        return spawnPoints[0];
    }
}
