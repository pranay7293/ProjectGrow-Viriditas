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

    public static readonly string[] characterFullNames = new string[]
    {
        "Indigo", "Astra Kim", "Dr. Cobalt Johnson", "Aspen Rodriguez", "Dr. Eden Kapoor",
        "Celeste Dubois", "Sierra Nakamura", "Lilith Fernandez", "River Osei", "Dr. Flora Tremblay"
    };

    private static readonly string[] characterShortNames = new string[]
    {
        "INDIGO", "ASTRA", "DR. COBALT", "ASPEN", "DR. EDEN",
        "CELESTE", "SIERRA", "LILITH", "RIVER", "DR. FLORA"
    };

    private void Start()
    {
        characterButtons = characterButtonsContainer.GetComponentsInChildren<Button>();

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = characterShortNames[i];
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
            if ((string)currentSelection == characterFullNames[index])
            {
                DeselectCharacter();
                return;
            }
        }

        string characterFullName = characterFullNames[index];
        Hashtable props = new Hashtable {{"SelectedCharacter", characterFullName}};
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
                    if ((string)selectedCharacter == characterFullNames[i])
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

    public static string GetShortName(string fullName)
    {
        int index = System.Array.IndexOf(characterFullNames, fullName);
        return index != -1 ? characterShortNames[index] : fullName;
    }
}