using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ScoringSystem : MonoBehaviourPunCallbacks
{
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private int collectiveScore = 0;

    public void UpdatePlayerScore(int playerActorNumber, int points)
    {
        if (playerScores.ContainsKey(playerActorNumber))
        {
            playerScores[playerActorNumber] += points;
        }
        else
        {
            playerScores[playerActorNumber] = points;
        }

        SyncScores();
    }

    public void UpdateCollectiveScore(int points)
    {
        collectiveScore += points;
        SyncScores();
    }

    private void SyncScores()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncScores", RpcTarget.All, playerScores, collectiveScore);
        }
    }

    [PunRPC]
    private void RPC_SyncScores(Dictionary<int, int> scores, int collective)
    {
        playerScores = scores;
        collectiveScore = collective;
        // Update UI or notify other systems about score changes
    }

    // Implement methods to calculate final scores, determine winners, etc.
}