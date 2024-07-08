using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    public GameObject characterButtonsContainer;
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;
    public Color takenColor = Color.red;
    public PlayerListManager playerListManager;

    private Button[] characterButtons;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    public static readonly string[] characterNames = new string[]
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

        UpdateCharacterButtonStates();
    }

    private void SelectCharacter(int index)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedCharacter", out object currentSelection))
        {
            if ((string)currentSelection == characterNames[index])
            {
                DeselectCharacter();
                return;
            }
        }

        string characterName = characterNames[index];
        Hashtable props = new Hashtable {{"SelectedCharacter", characterName}};
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateCharacterButtonStates();
    }

    private void DeselectCharacter()
    {
        PhotonNetwork.LocalPlayer.CustomProperties.Remove("SelectedCharacter");
        playerReadyStatus[PhotonNetwork.LocalPlayer.ActorNumber] = false;
        playerListManager.UpdatePlayerVotingStatus(PhotonNetwork.LocalPlayer.ActorNumber, false);
        UpdateCharacterButtonStates();
    }

    private void UpdateCharacterButtonStates()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            bool isTaken = false;
            bool isSelectedByLocalPlayer = false;

            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
                {
                    if ((string)selectedCharacter == characterNames[i])
                    {
                        isTaken = true;
                        isSelectedByLocalPlayer = player.IsLocal;
                        break;
                    }
                }
            }

            characterButtons[i].GetComponent<Image>().color = isSelectedByLocalPlayer ? selectedColor : (isTaken ? takenColor : defaultColor);
            characterButtons[i].interactable = !isTaken || isSelectedByLocalPlayer;
        }

        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        bool allReady = true;
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("SelectedCharacter"))
            {
                allReady = false;
                break;
            }
        }

        if (allReady && PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("TransitionScene");
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        UpdateCharacterButtonStates();
        playerListManager.UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        playerReadyStatus[newPlayer.ActorNumber] = false;
        playerListManager.UpdatePlayerList();
        UpdateCharacterButtonStates();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        playerListManager.UpdatePlayerList();
        UpdateCharacterButtonStates();
    }
}