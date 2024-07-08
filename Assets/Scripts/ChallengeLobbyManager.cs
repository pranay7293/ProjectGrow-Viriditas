using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class ChallengeLobbyManager : MonoBehaviourPunCallbacks
{
    public PlayerListManager playerListManager;
    public Button[] hubButtons;
    private const int MAX_PLAYERS = 5;

    private void Start()
    {
        SetHubButtonsInteractable(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        for (int i = 0; i < hubButtons.Length; i++)
        {
            int index = i;
            hubButtons[i].onClick.AddListener(() => SelectHub(index));
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon server.");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = MAX_PLAYERS };
        PhotonNetwork.JoinOrCreateRoom("ChallengeLobby", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        SetHubButtonsInteractable(true);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        playerListManager.UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        playerListManager.UpdatePlayerList();
    }

    private void SelectHub(int index)
    {
        // Set the selected hub index in the room properties
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties["SelectedHubIndex"] = index;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

        // Load the Challenges scene
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