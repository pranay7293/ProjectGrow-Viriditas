using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ChallengesManager : MonoBehaviourPunCallbacks
{
    public HubData[] allHubs;
    public GameObject challengeCardsContainer;
    public GameObject expandedChallengeContainer;
    public GameObject[] challengeCards;
    public GameObject[] expandedChallenges;
    public PlayerListManager playerListManager;

    private HubData currentHub;
    private int selectedChallengeIndex = -1;
    private int votedPlayersCount = 0;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        expandedChallengeContainer.SetActive(false);

        int selectedHubIndex = PlayerPrefs.GetInt("SelectedHubIndex", 0);
        currentHub = allHubs[selectedHubIndex];

        if (currentHub != null)
        {
            PopulateChallenges();
        }
        else
        {
            Debug.LogError("Selected hub not found!");
        }
    }

    private void PopulateChallenges()
    {
        for (int i = 0; i < challengeCards.Length; i++)
        {
            if (i < currentHub.challenges.Count)
            {
                challengeCards[i].SetActive(true);
                ChallengeCard card = challengeCards[i].GetComponent<ChallengeCard>();
                card.SetUp(currentHub.challenges[i], this, i);

                ExpandedChallengeCard expandedCard = expandedChallenges[i].GetComponent<ExpandedChallengeCard>();
                expandedCard.SetUp(currentHub.challenges[i], this, i);
            }
            else
            {
                challengeCards[i].SetActive(false);
                expandedChallenges[i].SetActive(false);
            }
        }
    }

    public void ExpandChallenge(int index)
    {
        challengeCardsContainer.SetActive(false);
        expandedChallengeContainer.SetActive(true);

        for (int i = 0; i < expandedChallenges.Length; i++)
        {
            expandedChallenges[i].SetActive(i == index);
        }
    }

    public void CollapseChallenge()
    {
        challengeCardsContainer.SetActive(true);
        expandedChallengeContainer.SetActive(false);
    }

    public void OnChallengeSelected(int challengeIndex)
    {
        selectedChallengeIndex = challengeIndex;
        ChallengeCard selectedCard = challengeCards[challengeIndex].GetComponent<ChallengeCard>();
        string challengeTitle = selectedCard.GetChallengeTitle();
        
        // Update room properties
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
        {
            { "SelectedChallengeTitle", challengeTitle }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

        // Update PlayerPrefs as a fallback
        PlayerPrefs.SetString("SelectedChallengeTitle", challengeTitle);
        PlayerPrefs.Save();

        photonView.RPC("UpdateVote", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, selectedChallengeIndex);
    }

    [PunRPC]
    private void UpdateVote(int playerActorNumber, int challengeIndex)
    {
        votedPlayersCount++;
        playerListManager.UpdatePlayerVotingStatus(playerActorNumber, true);

        if (votedPlayersCount == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            // All players have voted, proceed to the CharacterSelection scene
            PhotonNetwork.LoadLevel("CharacterSelection");
        }
    }
}

// using UnityEngine;
// using Photon.Pun;
// using Photon.Realtime;

// public class ChallengesManager : MonoBehaviourPunCallbacks, IPunObservable
// {
//     public GameObject[] challengeCardObjects;
//     public PlayerListManager playerListManager;

//     private ChallengeCard[] challengeCards;
//     private int selectedChallengeIndex = -1;
//     private int votedPlayersCount = 0;

//     private void Start()
//     {
//         if (!PhotonNetwork.IsConnected)
//         {
//             PhotonNetwork.ConnectUsingSettings();
//         }

//         if (challengeCardObjects == null || challengeCardObjects.Length == 0)
//         {
//             Debug.LogError("Challenge card objects are not properly assigned in the Inspector.");
//             return;
//         }

//         challengeCards = new ChallengeCard[challengeCardObjects.Length];
//         for (int i = 0; i < challengeCardObjects.Length; i++)
//         {
//             challengeCards[i] = challengeCardObjects[i].GetComponent<ChallengeCard>();
//             if (challengeCards[i] == null)
//             {
//                 Debug.LogError("ChallengeCard component not found on the challenge card object at index " + i);
//                 return;
//             }
//         }
//     }

//     public override void OnConnectedToMaster()
//     {
//         Debug.Log("Connected to Photon server.");
//         if (PhotonNetwork.CurrentRoom == null)
//         {
//             PhotonNetwork.JoinRoom("ChallengeLobby");
//         }
//         else
//         {
//             Debug.Log("Already in a room: " + PhotonNetwork.CurrentRoom.Name);
//             SetupChallengeRoom();
//         }
//     }

//     public override void OnJoinedRoom()
//     {
//         Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
//         SetupChallengeRoom();
//     }

//     private void SetupChallengeRoom()
//     {
//         if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedHubIndex", out object selectedHubIndexObj))
//         {
//             int selectedHubIndex = (int)selectedHubIndexObj;
//             SetChallenges(selectedHubIndex);
//         }
//         else
//         {
//             Debug.LogError("Selected hub index not found in room properties.");
//         }

//         if (playerListManager == null)
//         {
//             Debug.LogError("PlayerListManager is not properly assigned in the Inspector.");
//             return;
//         }

//         playerListManager.UpdatePlayerList();
//     }

//     private void SetChallenges(int hubIndex)
//     {
//         // Set the challenge names based on the selected hub index
//         // Example:
//         // challengeCards[0].SetChallengeDetails("Challenge 1");
//         // challengeCards[1].SetChallengeDetails("Challenge 2");
//         // challengeCards[2].SetChallengeDetails("Challenge 3");
//     }

//     public void OnChallengeExpanded(ChallengeCard expandedChallengeCard)
//     {
//         foreach (ChallengeCard challengeCard in challengeCards)
//         {
//             if (challengeCard != expandedChallengeCard)
//             {
//                 challengeCard.gameObject.SetActive(false);
//             }
//         }
//     }

//     public void OnChallengeCollapsed()
//     {
//         foreach (ChallengeCard challengeCard in challengeCards)
//         {
//             challengeCard.gameObject.SetActive(true);
//         }
//     }

//     public void OnChallengeSelected(int challengeIndex)
//     {
//         selectedChallengeIndex = challengeIndex;
//         photonView.RPC("UpdateVote", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, selectedChallengeIndex);
//     }

//    [PunRPC]
//     private void UpdateVote(int playerActorNumber, int challengeIndex)
//     {
//         votedPlayersCount++;
//         playerListManager.UpdatePlayerVotingStatus(playerActorNumber, true);

//         if (votedPlayersCount == PhotonNetwork.CurrentRoom.PlayerCount)
//         {
//             // All players have voted, proceed to the CharacterSelection scene
//             PhotonNetwork.LoadLevel("CharacterSelection");
//         }
//     }

//     public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             stream.SendNext(selectedChallengeIndex);
//             stream.SendNext(votedPlayersCount);
//         }
//         else
//         {
//             selectedChallengeIndex = (int)stream.ReceiveNext();
//             votedPlayersCount = (int)stream.ReceiveNext();
//         }
//     }
// }