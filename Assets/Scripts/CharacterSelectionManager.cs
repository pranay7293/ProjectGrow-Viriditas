using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    public GameObject characterButtonsContainer;
    public PlayerProfileManager playerProfileManager;
    public float hoverTransitionDuration = 0.3f;

    [System.Serializable]
    public class CharacterButton
    {
        public Button button;
        public Color hoverColor;
        [HideInInspector] public Color defaultColor;
    }

    public List<CharacterButton> characterButtons = new List<CharacterButton>();

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
        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i;
            Button button = characterButtons[i].button;
            button.GetComponentInChildren<TextMeshProUGUI>().text = characterShortNames[i];
            
            // Store the default color
            Image buttonImage = button.GetComponent<Image>();
            characterButtons[i].defaultColor = buttonImage.color;

            button.onClick.AddListener(() => SelectCharacter(index));
            
            // Add event trigger components for hover effects
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data, index); });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data, index); });
            trigger.triggers.Add(exitEntry);
        }

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            playerReadyStatus[player.ActorNumber] = false;
        }

        UpdateCharacterButtonStates();
    }

    private void SelectCharacter(int index)
    {
        if (characterShortNames[index] == "LILITH")
        {
            // For Lilith, just show the Observer object
            characterButtons[index].button.transform.Find("Observer").gameObject.SetActive(true);
            return;
        }

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
        playerProfileManager.UpdatePlayerVotingStatus(PhotonNetwork.LocalPlayer.ActorNumber, false);
        UpdateCharacterButtonStates();
    }

    private void UpdateCharacterButtonStates()
    {
        for (int i = 0; i < characterButtons.Count; i++)
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

            Image buttonImage = characterButtons[i].button.GetComponent<Image>();
            if (isTaken)
            {
                buttonImage.color = characterButtons[i].hoverColor;
            }
            else
            {
                buttonImage.color = characterButtons[i].defaultColor;
            }

            characterButtons[i].button.interactable = !isTaken || isSelectedByLocalPlayer;
        }

        // Special handling for Lilith
        int lilithIndex = System.Array.IndexOf(characterShortNames, "LILITH");
        if (lilithIndex != -1)
        {
            characterButtons[lilithIndex].button.interactable = true;
            characterButtons[lilithIndex].button.transform.Find("Observer").gameObject.SetActive(false);
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
        playerProfileManager.UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        playerReadyStatus[newPlayer.ActorNumber] = false;
        playerProfileManager.UpdatePlayerList();
        UpdateCharacterButtonStates();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        playerProfileManager.UpdatePlayerList();
        UpdateCharacterButtonStates();
    }

    public static string GetShortName(string fullName)
    {
        int index = System.Array.IndexOf(characterFullNames, fullName);
        return index != -1 ? characterShortNames[index] : fullName;
    }

    private void OnPointerEnter(PointerEventData eventData, int index)
    {
        if (characterShortNames[index] == "LILITH")
        {
            characterButtons[index].button.transform.Find("Observer").gameObject.SetActive(true);
        }
        else
        {
            Image buttonImage = characterButtons[index].button.GetComponent<Image>();
            buttonImage.DOColor(characterButtons[index].hoverColor, hoverTransitionDuration);
        }
    }

    private void OnPointerExit(PointerEventData eventData, int index)
    {
        if (characterShortNames[index] == "LILITH")
        {
            characterButtons[index].button.transform.Find("Observer").gameObject.SetActive(false);
        }
        else
        {
            Image buttonImage = characterButtons[index].button.GetComponent<Image>();
            if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter) 
                || (string)selectedCharacter != characterFullNames[index])
            {
                buttonImage.DOColor(characterButtons[index].defaultColor, hoverTransitionDuration);
            }
        }
    }
}