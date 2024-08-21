using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ChallengesManager : MonoBehaviourPunCallbacks
{
    public HubData[] allHubs;
    public GameObject challengeCardsContainer;
    public GameObject expandedChallengeContainer;
    public GameObject[] challengeCards;
    public GameObject[] expandedChallenges;
    public PlayerProfileManager playerProfileManager;
    public TextMeshProUGUI mainTitleText;
    public Sprite lockedChallengeSprite;
    public Button backButton;

    [Range(0f, 1f)]
    public float middleCardDarkenAmount = 0.1f;

    private HubData currentHub;
    private int selectedChallengeIndex = -1;
    private int votedPlayersCount = 0;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("Not connected to Photon or not in a room. Returning to ChallengeLobby.");
            PhotonNetwork.LoadLevel("ChallengeLobby");
            return;
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

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToChallengeLobby);
        }
        else
        {
            Debug.LogWarning("Back button is not assigned in the inspector!");
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
                
                if (i == 1)
                {
                    cardColor = DarkenColor(cardColor, middleCardDarkenAmount);
                }
                
                bool isAvailable = (i == currentHub.availableChallengeIndex);
                Sprite iconSprite = isAvailable ? currentHub.challenges[i].iconSprite : lockedChallengeSprite;
                
                card.SetUp(currentHub.challenges[i], this, i, cardColor, isAvailable, iconSprite);

                ExpandedChallengeCard expandedCard = expandedChallenges[i].GetComponent<ExpandedChallengeCard>();
                expandedCard.SetUp(currentHub.challenges[i], this, i, cardColor, isAvailable, currentHub.useInvertedColors);
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

        mainTitleText.gameObject.SetActive(false);
    }

    public void CollapseChallenge()
    {
        challengeCardsContainer.SetActive(true);
        expandedChallengeContainer.SetActive(false);
        mainTitleText.gameObject.SetActive(true);
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
        playerProfileManager.UpdatePlayerVotingStatus(playerActorNumber, true);

        if (votedPlayersCount == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            PhotonNetwork.LoadLevel("CharacterSelection");
        }
    }
    
private void GoBackToChallengeLobby()
    {
        PlayerPrefs.DeleteKey("SelectedHubIndex");
        PlayerPrefs.Save();

        PhotonNetwork.LoadLevel("ChallengeLobby");
    }
}