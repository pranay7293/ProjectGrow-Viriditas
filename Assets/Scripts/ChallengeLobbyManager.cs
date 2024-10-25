using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class ChallengeLobbyManager : MonoBehaviourPunCallbacks
{
    public PlayerProfileManager playerProfileManager;
    public Button[] hubButtons;
    private const int MAX_PLAYERS = 5;

    private void Start()
    {
        SetHubButtonsInteractable(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            OnConnectedToMaster();
        }

        for (int i = 0; i < hubButtons.Length; i++)
        {
            int index = i;
            hubButtons[i].onClick.AddListener(() => SelectHub(index));
        }
    }

    public override void OnConnectedToMaster()
    {
        // Debug.Log("Connected to Photon server.");
        if (!PhotonNetwork.InRoom)
        {
            RoomOptions roomOptions = new RoomOptions { MaxPlayers = MAX_PLAYERS };
            PhotonNetwork.JoinOrCreateRoom("ChallengeLobby", roomOptions, TypedLobby.Default);
        }
        else
        {
            OnJoinedRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        // Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        SetHubButtonsInteractable(true);
        playerProfileManager.UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        playerProfileManager.UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        playerProfileManager.UpdatePlayerList();
    }

    private void SelectHub(int index)
    {
        PlayerPrefs.SetInt("SelectedHubIndex", index);
        PlayerPrefs.Save();

        PhotonNetwork.LoadLevel("Challenges");
    }

    private void SetHubButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < hubButtons.Length; i++)
        {
            hubButtons[i].interactable = interactable;
        }
    }
}