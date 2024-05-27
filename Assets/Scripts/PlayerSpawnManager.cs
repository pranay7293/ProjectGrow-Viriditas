using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawnManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        int selectedCharacterIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["SelectedCharacterIndex"];
        Transform spawnPoint = spawnPoints[selectedCharacterIndex];
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }
}