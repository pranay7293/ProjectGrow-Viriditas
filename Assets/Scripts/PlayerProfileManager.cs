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
        foreach (GameObject placeholder in playerPlaceholders)
        {
            placeholder.SetActive(false);
        }

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length && i < playerPlaceholders.Length; i++)
        {
            GameObject placeholder = playerPlaceholders[i];
            placeholder.SetActive(true);
            UpdatePlayerListItem(placeholder, players[i]);
        }
    }

    private void UpdatePlayerListItem(GameObject placeholder, Player player)
    {
        TextMeshProUGUI playerNameText = placeholder.GetComponentInChildren<TextMeshProUGUI>();
        if (playerNameText != null)
        {
            playerNameText.text = player.IsLocal ? "ME" : player.NickName;
        }
    }

    public void InitializeProfiles()
    {
        isGameScene = true;
        UpdateGameScenePlayerList();
    }

    private void UpdateGameScenePlayerList()
    {
        if (playerListContainer == null)
        {
            Debug.LogError("PlayerListContainer is not assigned in PlayerProfileManager");
            return;
        }
        playerListContainer.gameObject.SetActive(true);

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

    private void CreateCharacterProfile(string characterName)
    {
        if (playerProfilePrefab == null)
        {
            Debug.LogError("PlayerProfilePrefab is not assigned in PlayerProfileManager");
            return;
        }

        GameObject profileObj = Instantiate(playerProfilePrefab, playerListContainer);
        PlayerProfileUI profileUI = profileObj.GetComponent<PlayerProfileUI>();

        if (profileUI == null)
        {
            Debug.LogError($"PlayerProfileUI component not found on instantiated prefab for character: {characterName}");
            return;
        }

        UniversalCharacterController character = GameManager.Instance.GetCharacterByName(characterName);
        if (character != null)
        {
            bool isAI = !character.IsPlayerControlled;
            bool isLocalPlayer = character.photonView.IsMine && character.IsPlayerControlled;
            profileUI.SetPlayerInfo(characterName, character.characterColor, isAI, isLocalPlayer);
        }
        else
        {
            Debug.LogWarning($"Character not found: {characterName}. Setting default values.");
            profileUI.SetPlayerInfo(characterName, Color.gray, true, false);
        }

        playerProfiles[characterName] = profileUI;
    }

    private void SortPlayersByScore()
    {
        var sortedProfiles = playerProfiles.OrderByDescending(kvp => GameManager.Instance.GetPlayerScore(kvp.Key)).ToList();

        for (int i = 0; i < sortedProfiles.Count; i++)
        {
            sortedProfiles[i].Value.transform.SetSiblingIndex(i);
        }
    }

    public void UpdatePlayerProgress(string characterName, float overallProgress, float[] personalProgress)
    {
        if (playerProfiles.TryGetValue(characterName, out PlayerProfileUI profile))
        {
            profile.UpdatePersonalGoals(personalProgress);
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
}