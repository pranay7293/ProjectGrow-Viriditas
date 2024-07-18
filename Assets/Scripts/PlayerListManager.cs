using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class PlayerListManager : MonoBehaviourPunCallbacks
{
    public GameObject[] playerPlaceholders;
    [SerializeField] private bool isGameScene = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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

            if (isGameScene)
            {
                UpdatePlayerProfile(placeholder, players[i]);
            }
            else
            {
                UpdatePlayerListItem(placeholder, players[i]);
            }
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

    private void UpdatePlayerProfile(GameObject placeholder, Player player)
    {
        TextMeshProUGUI playerNameText = placeholder.GetComponentInChildren<TextMeshProUGUI>();
        Image avatarImage = placeholder.GetComponentInChildren<Image>();
        Slider progressBar = placeholder.GetComponentInChildren<Slider>();

        if (playerNameText != null)
        {
            playerNameText.text = player.IsLocal ? "ME" : player.NickName;
        }

        if (avatarImage != null)
        {
            // TODO: Set avatar image based on player data
        }

        if (progressBar != null)
        {
            // TODO: Set progress based on player score
            // progressBar.value = GetPlayerProgress(player);
        }
    }

    public void UpdatePlayerVotingStatus(int playerActorNumber, bool hasVoted)
    {
        if (playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
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
        if (!isGameScene || playerActorNumber <= 0 || playerActorNumber > playerPlaceholders.Length)
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