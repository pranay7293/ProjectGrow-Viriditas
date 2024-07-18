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