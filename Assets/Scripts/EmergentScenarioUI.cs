using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using DG.Tweening;
using UnityEngine.EventSystems;

public class EmergentScenarioUI : MonoBehaviourPunCallbacks
{
    public static EmergentScenarioUI Instance { get; private set; }

    [SerializeField] private GameObject scenarioPanel;
    [SerializeField] private TextMeshProUGUI[] scenarioTexts;
    [SerializeField] private Button[] scenarioButtons;
    [SerializeField] private Transform[] profileContainers;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI whatIfText;
    [SerializeField] private GameObject characterProfilePrefab;
    [SerializeField] private EmergentScenarioNotification emergentScenarioNotification;
    [SerializeField] private float hoverTransitionDuration = 0.3f;

    private Color hubColor;
    private Color defaultColor = new Color(0.094f, 0.094f, 0.094f); // #181818 in RGB
    private Dictionary<int, List<CharacterProfileSimple>> scenarioProfiles = new Dictionary<int, List<CharacterProfileSimple>>();
    private Dictionary<string, int> playerVotes = new Dictionary<string, int>();
    private float voteTimer = 30f;
    private bool isVoting = false;
    private int localPlayerVote = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        for (int i = 0; i < scenarioButtons.Length; i++)
        {
            int index = i;
            scenarioButtons[i].onClick.AddListener(() => OnScenarioSelected(index));
            
            EventTrigger trigger = scenarioButtons[i].gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data, index); });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data, index); });
            trigger.triggers.Add(exitEntry);

            scenarioButtons[i].GetComponent<Image>().color = defaultColor;
        }
        scenarioPanel.SetActive(false);
    }

    public void DisplayScenarios(List<string> scenarios)
    {
        InputManager.Instance.SetUIActive(true);
        hubColor = GameManager.Instance.GetCurrentHubColor();
        Debug.Log($"Current Hub Color: R={hubColor.r}, G={hubColor.g}, B={hubColor.b}, A={hubColor.a}");
        scenarioPanel.SetActive(true);

        for (int i = 0; i < scenarioTexts.Length; i++)
        {
            scenarioTexts[i].text = i < scenarios.Count ? scenarios[i] : "";
            scenarioButtons[i].gameObject.SetActive(i < scenarios.Count);
        }
        
        whatIfText.text = "What If...";
        timerText.text = $"{Mathf.CeilToInt(voteTimer)}";
        
        StartVoting();
    }

    private void StartVoting()
    {
        playerVotes.Clear();
        voteTimer = 30f;
        isVoting = true;
        localPlayerVote = -1;
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SimulateAIVoting());
        }
    }

    private void OnScenarioSelected(int scenarioIndex)
    {
        if (!isVoting) return;

        string characterName = GetLocalPlayerCharacterName();
        if (!string.IsNullOrEmpty(characterName))
        {
            localPlayerVote = scenarioIndex;
            photonView.RPC("RPC_SubmitVote", RpcTarget.All, scenarioIndex, characterName);
        }
        else
        {
            Debug.LogError("Local player's character name not found.");
        }
    }

    private string GetLocalPlayerCharacterName()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
        {
            return (string)selectedCharacter;
        }
        return null;
    }

    [PunRPC]
    private void RPC_SubmitVote(int scenarioIndex, string characterName)
    {
        if (!playerVotes.ContainsKey(characterName))
        {
            playerVotes[characterName] = scenarioIndex;
            DisplayVote(characterName, scenarioIndex);
        }
    }

    private void DisplayVote(string characterName, int scenarioIndex)
    {
        UniversalCharacterController character = GameManager.Instance.GetCharacterByName(characterName);
        if (character != null)
        {
            CharacterProfileSimple profile = Instantiate(characterProfilePrefab, profileContainers[scenarioIndex]).GetComponent<CharacterProfileSimple>();
            profile.SetProfileInfo(characterName, character.GetCharacterColor(), !character.IsPlayerControlled, character.photonView.IsMine);
            
            int voteCount = profileContainers[scenarioIndex].childCount;
            ResizeProfileContainer(scenarioIndex, voteCount);
        }
    }

    private void ResizeProfileContainer(int scenarioIndex, int voteCount)
    {
        if (profileContainers[scenarioIndex] != null)
        {
            RectTransform containerRect = profileContainers[scenarioIndex].GetComponent<RectTransform>();
            GridLayoutGroup grid = profileContainers[scenarioIndex].GetComponent<GridLayoutGroup>();
            
            if (containerRect != null && grid != null)
            {
                int rows = Mathf.CeilToInt(voteCount / 5f);
                float newHeight = rows * (grid.cellSize.y + grid.spacing.y);
                containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            }
        }
    }

    private IEnumerator SimulateAIVoting()
    {
        List<string> aiCharacters = GameManager.Instance.GetAICharacterNames();
        foreach (string aiName in aiCharacters)
        {
            yield return new WaitForSeconds(Random.Range(1f, 5f));
            if (isVoting && !playerVotes.ContainsKey(aiName))
            {
                AIManager aiManager = GameManager.Instance.GetCharacterByName(aiName).GetComponent<AIManager>();
                List<string> scenarioDescriptions = GetCurrentScenarios();
                GameState currentGameState = GameManager.Instance.GetCurrentGameState();
                int chosenScenario = aiManager.DecideScenario(scenarioDescriptions, currentGameState);
                photonView.RPC("RPC_SubmitVote", RpcTarget.All, chosenScenario, aiName);
            }
        }
    }

    private void Update()
    {
        if (isVoting)
        {
            voteTimer -= Time.deltaTime;
            timerText.text = $"{Mathf.CeilToInt(voteTimer)}";

            if (voteTimer <= 0)
            {
                EndVoting();
            }
        }
    }

    private void EndVoting()
    {
        isVoting = false;
        int winningScenario = DetermineWinningScenario();
        string winningScenarioText = scenarioTexts[winningScenario].text;
        
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_ImplementWinningScenario", RpcTarget.All, winningScenarioText);
        }
    }

    [PunRPC]
    private void RPC_ImplementWinningScenario(string winningScenarioText)
    {
        StartCoroutine(ImplementWinningScenarioSequence(winningScenarioText));
    }

private IEnumerator ImplementWinningScenarioSequence(string winningScenarioText)
{
    yield return StartCoroutine(HideScenarioPanel());
    GameManager.Instance.ResetPlayerPositions();
    GameManager.Instance.ImplementEmergentScenario(winningScenarioText);
    emergentScenarioNotification.DisplayNotification(winningScenarioText);
    yield return new WaitForSeconds(emergentScenarioNotification.GetNotificationDuration());
    GameManager.Instance.EndEmergentScenario();
    InputManager.Instance.SetUIActive(false);
}

    private IEnumerator HideScenarioPanel()
    {
        scenarioPanel.SetActive(false);
        yield return null;
    }

    private int DetermineWinningScenario()
    {
        if (playerVotes.Count == 0) return 0;

        var voteCounts = new Dictionary<int, int>();
        foreach (var vote in playerVotes.Values)
        {
            if (!voteCounts.ContainsKey(vote))
                voteCounts[vote] = 0;
            voteCounts[vote]++;
        }

        int maxVotes = voteCounts.Values.Max();
        var winningScenarios = new List<int>();
        foreach (var kvp in voteCounts)
        {
            if (kvp.Value == maxVotes)
                winningScenarios.Add(kvp.Key);
        }

        return winningScenarios[Random.Range(0, winningScenarios.Count)];
    }

    private List<string> GetCurrentScenarios()
    {
        return new List<string>(scenarioTexts.Select(text => text.text));
    }

    private void OnPointerEnter(PointerEventData eventData, int index)
    {
        if (!isVoting) return;
        Image buttonImage = scenarioButtons[index].GetComponent<Image>();
        buttonImage.DOColor(hubColor, hoverTransitionDuration);
    }

    private void OnPointerExit(PointerEventData eventData, int index)
    {
        if (!isVoting) return;
        Image buttonImage = scenarioButtons[index].GetComponent<Image>();
        if (localPlayerVote != index)
        {
            buttonImage.DOColor(defaultColor, hoverTransitionDuration);
        }
    }
}