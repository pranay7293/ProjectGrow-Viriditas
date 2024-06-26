using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    public GameObject characterButtonsContainer;
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;
    public PlayerListManager playerListManager;
    public UniversalCharacterController characterPrefab;
    public Transform[] spawnPoints;

    private Button[] characterButtons;
    private int selectedCharacterIndex = -1;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    private string[] characterNames = new string[]
    {
        "Dr. Flora Tremblay", "Sierra Nakamura", "Dr. Eden Kapoor", "Indigo", "Dr. Cobalt Johnson",
        "Aspen Rodriguez", "River Osei", "Celeste Dubois", "Astra Kim", "Lilith Fernandez"
    };

    private void Start()
    {
        characterButtons = characterButtonsContainer.GetComponentsInChildren<Button>();

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = characterNames[i];
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            playerReadyStatus[player.ActorNumber] = false;
        }
    }

    private void SelectCharacter(int index)
    {
        if (selectedCharacterIndex == index)
        {
            DeselectCharacter();
        }
        else
        {
            DeselectCharacter();

            selectedCharacterIndex = index;
            characterButtons[index].GetComponent<Image>().color = selectedColor;

            string characterName = characterNames[index];
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps["SelectedCharacter"] = characterName;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            photonView.RPC("UpdateCharacterSelection", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, index, characterName);

            playerReadyStatus[PhotonNetwork.LocalPlayer.ActorNumber] = true;
            playerListManager.UpdatePlayerVotingStatus(PhotonNetwork.LocalPlayer.ActorNumber, true);

            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                StartGame();
            }
            else if (CheckAllPlayersReady())
            {
                StartGame();
            }
        }
    }

    private void DeselectCharacter()
    {
        if (selectedCharacterIndex >= 0)
        {
            characterButtons[selectedCharacterIndex].GetComponent<Image>().color = defaultColor;
            selectedCharacterIndex = -1;

            playerReadyStatus[PhotonNetwork.LocalPlayer.ActorNumber] = false;
            playerListManager.UpdatePlayerVotingStatus(PhotonNetwork.LocalPlayer.ActorNumber, false);
        }
    }

    [PunRPC]
    private void UpdateCharacterSelection(int playerActorNumber, int characterIndex, string characterName)
    {
        characterButtons[characterIndex].GetComponent<Image>().color = Color.red;

        Photon.Realtime.Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
        if (player != null)
        {
            characterButtons[characterIndex].GetComponentInChildren<TextMeshProUGUI>().text = player.NickName + " - " + characterName;
        }

        playerReadyStatus[playerActorNumber] = true;
        playerListManager.UpdatePlayerVotingStatus(playerActorNumber, true);
    }

    private bool CheckAllPlayersReady()
    {
        foreach (bool isReady in playerReadyStatus.Values)
        {
            if (!isReady)
            {
                return false;
            }
        }
        return true;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        playerListManager.UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        playerListManager.UpdatePlayerList();
    }

    private void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("TransitionScene");
        }
    }
}