using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class ChallengesManager : MonoBehaviourPunCallbacks
{
    public HubData[] allHubs;
    public GameObject challengeCardsContainer;
    public GameObject expandedChallengeContainer;
    public GameObject[] challengeCards;
    public GameObject[] expandedChallenges;
    public PlayerListManager playerListManager;

    [Range(0f, 1f)]
    public float middleCardDarkenAmount = 0.1f;

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
                Color cardColor = currentHub.hubColor;
                
                // Darken the middle card
                if (i == 1)
                {
                    cardColor = DarkenColor(cardColor, middleCardDarkenAmount);
                }
                
                card.SetUp(currentHub.challenges[i], this, i, cardColor);

                ExpandedChallengeCard expandedCard = expandedChallenges[i].GetComponent<ExpandedChallengeCard>();
                expandedCard.SetUp(currentHub.challenges[i], this, i, cardColor);
            }
            else
            {
                challengeCards[i].SetActive(false);
                expandedChallenges[i].SetActive(false);
            }
        }
    }

    private Color DarkenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a
        );
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
        
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
        {
            { "SelectedChallengeTitle", challengeTitle }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

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
            PhotonNetwork.LoadLevel("CharacterSelection");
        }
    }
}