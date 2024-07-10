using UnityEngine;
using TMPro;
using Photon.Pun;

public class ChallengeDisplayManager : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI challengeTitleText;

    private void Start()
    {
        UpdateChallengeDisplay();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        UpdateChallengeDisplay();
    }

    private void UpdateChallengeDisplay()
    {
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedChallengeTitle", out object challengeTitle))
        {
            challengeTitleText.text = (string)challengeTitle;
        }
        else
        {
            // Fallback to PlayerPrefs if room property is not set
            string savedChallengeTitle = PlayerPrefs.GetString("SelectedChallengeTitle", "Model Real-World Synbio Challenge");
            challengeTitleText.text = savedChallengeTitle;
        }
    }
}