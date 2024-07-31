using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections.Generic;

public class PlayerProfileManager : MonoBehaviourPunCallbacks
{
    public static PlayerProfileManager Instance { get; private set; }

    [SerializeField] private bool isGameScene = false;
    [SerializeField] private GameObject playerProfilePrefab;
    [SerializeField] private RectTransform playerListContainer;
    [SerializeField] private GameObject[] playerPlaceholders;

    private Dictionary<string, PlayerProfileUI> playerProfiles = new Dictionary<string, PlayerProfileUI>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (isGameScene && playerListContainer != null)
        {
            playerListContainer.gameObject.SetActive(true);
        }
        UpdatePlayerList();
    }

    public override void OnJoinedRoom()
    {
        UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        if (isGameScene)
        {
            UpdateGameScenePlayerList();
        }
        else
        {
            UpdateNonGameScenePlayerList();
        }
    }

    private void UpdateNonGameScenePlayerList()
    {
        if (playerPlaceholders == null) return;

        foreach (GameObject placeholder in playerPlaceholders)
        {
            placeholder.SetActive(false);
        }

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (i >= playerPlaceholders.Length)
            {
                break;
            }

            GameObject placeholder = playerPlaceholders[i];
            placeholder.SetActive(true);
            UpdatePlayerListItem(placeholder, players[i]);
        }
    }

    private void UpdateGameScenePlayerList()
    {
        if (playerListContainer == null) return;

        foreach (var profile in playerProfiles.Values)
        {
            Destroy(profile.gameObject);
        }
        playerProfiles.Clear();

        foreach (string characterName in CharacterSelectionManager.characterFullNames)
        {
            CreateCharacterProfile(characterName);
        }

        SortPlayersByScore();
    }

    private void UpdatePlayerListItem(GameObject placeholder, Player player)
    {
        TextMeshProUGUI playerNameText = placeholder.GetComponentInChildren<TextMeshProUGUI>();
        if (playerNameText != null)
        {
            playerNameText.text = player.IsLocal ? "ME" : player.NickName;
        }
    }

    private void CreateCharacterProfile(string characterName)
    {
        GameObject profileObj = Instantiate(playerProfilePrefab, playerListContainer);
        PlayerProfileUI profileUI = profileObj.GetComponent<PlayerProfileUI>();

        UniversalCharacterController character = GameManager.Instance.GetCharacterByName(characterName);
        if (character != null)
        {
            bool isAI = !character.IsPlayerControlled;
            profileUI.SetPlayerInfo(characterName, character.characterColor, isAI);
            profileUI.SetLocalPlayer(character.photonView.IsMine);
        }
        else
        {
            Debug.LogWarning($"Character not found: {characterName}");
        }

        playerProfiles[characterName] = profileUI;
    }

    private void SortPlayersByScore()
    {
        if (playerListContainer == null) return;

        var sortedProfiles = playerProfiles.OrderByDescending(kvp => GameManager.Instance.GetPlayerScore(kvp.Key)).ToList();

        for (int i = 0; i < sortedProfiles.Count; i++)
        {
            sortedProfiles[i].Value.transform.SetSiblingIndex(i);
        }
    }

    public void UpdatePlayerProgress(string playerName, float overallProgress, float personalProgress)
    {
        if (!isGameScene) return;
        if (playerProfiles.TryGetValue(playerName, out PlayerProfileUI profile))
        {
            profile.UpdateProgress(overallProgress, personalProgress);
        }
    }

    public void UpdatePlayerInsights(string playerName, int insightCount)
    {
        if (!isGameScene) return;
        if (playerProfiles.TryGetValue(playerName, out PlayerProfileUI profile))
        {
            profile.UpdateInsights(insightCount);
        }
    }

    public void UpdatePlayerVotingStatus(int playerActorNumber, bool hasVoted)
    {
        if (playerPlaceholders == null || playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
        {
            return;
        }

        GameObject placeholder = playerPlaceholders[playerActorNumber - 1];
        Image placeholderImage = placeholder.GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.color = hasVoted ? new Color(0x0D / 255f, 0x81 / 255f, 0x57 / 255f) : Color.white;
        }
    }

    public void UpdatePlayerProgress(int playerActorNumber, float progress)
    {
        if (!isGameScene || playerPlaceholders == null || playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
        {
            return;
        }

        GameObject placeholder = playerPlaceholders[playerActorNumber - 1];
        Slider progressBar = placeholder.GetComponentInChildren<Slider>();
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
}


// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using Photon.Pun;
// using Photon.Realtime;

// public class PlayerListManager : MonoBehaviourPunCallbacks
// {
//     public GameObject[] playerPlaceholders;
//     [SerializeField] private bool isGameScene = false;

//     private void Awake()
//     {
//         DontDestroyOnLoad(gameObject);
//     }

//     private void Start()
//     {
//         UpdatePlayerList();
//     }

//     public override void OnJoinedRoom()
//     {
//         UpdatePlayerList();
//     }

//     public override void OnPlayerEnteredRoom(Player newPlayer)
//     {
//         UpdatePlayerList();
//     }

//     public override void OnPlayerLeftRoom(Player otherPlayer)
//     {
//         UpdatePlayerList();
//     }

//     public void UpdatePlayerList()
//     {
//         foreach (GameObject placeholder in playerPlaceholders)
//         {
//             placeholder.SetActive(false);
//         }

//         Player[] players = PhotonNetwork.PlayerList;
//         for (int i = 0; i < players.Length; i++)
//         {
//             if (i >= playerPlaceholders.Length)
//             {
//                 break;
//             }

//             GameObject placeholder = playerPlaceholders[i];
//             placeholder.SetActive(true);

//             if (isGameScene)
//             {
//                 UpdatePlayerProfile(placeholder, players[i]);
//             }
//             else
//             {
//                 UpdatePlayerListItem(placeholder, players[i]);
//             }
//         }
//     }

//     private void UpdatePlayerListItem(GameObject placeholder, Player player)
//     {
//         TextMeshProUGUI playerNameText = placeholder.GetComponentInChildren<TextMeshProUGUI>();
//         if (playerNameText != null)
//         {
//             playerNameText.text = player.IsLocal ? "ME" : player.NickName;
//         }
//     }

//     private void UpdatePlayerProfile(GameObject placeholder, Player player)
//     {
//         TextMeshProUGUI playerNameText = placeholder.GetComponentInChildren<TextMeshProUGUI>();
//         Image avatarImage = placeholder.GetComponentInChildren<Image>();
//         Slider progressBar = placeholder.GetComponentInChildren<Slider>();

//         if (playerNameText != null)
//         {
//             playerNameText.text = player.IsLocal ? "ME" : player.NickName;
//         }

//         if (avatarImage != null)
//         {
//             // TODO: Set avatar image based on player data
//         }

//         if (progressBar != null)
//         {
//             // TODO: Set progress based on player score
//             // progressBar.value = GetPlayerProgress(player);
//         }
//     }

//     public void UpdatePlayerVotingStatus(int playerActorNumber, bool hasVoted)
//     {
//         if (playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
//         {
//             return;
//         }

//         GameObject placeholder = playerPlaceholders[playerActorNumber - 1];
//         Image placeholderImage = placeholder.GetComponent<Image>();
//         if (placeholderImage != null)
//         {
//             placeholderImage.color = hasVoted ? new Color(0x0D / 255f, 0x81 / 255f, 0x57 / 255f) : Color.white;
//         }
//     }

//     public void UpdatePlayerProgress(int playerActorNumber, float progress)
//     {
//         if (!isGameScene || playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
//         {
//             return;
//         }

//         GameObject placeholder = playerPlaceholders[playerActorNumber - 1];
//         Slider progressBar = placeholder.GetComponentInChildren<Slider>();
//         if (progressBar != null)
//         {
//             progressBar.value = progress;
//         }
//     }
// }