using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class EmergentScenarioUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject scenarioPanel;
    [SerializeField] private TextMeshProUGUI[] scenarioTexts;
    [SerializeField] private Button[] scenarioButtons;
    [SerializeField] private Transform[] profileContainers;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI whatIfText;
    [SerializeField] private GameObject characterProfilePrefab;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private EmergentScenarioNotification emergentScenarioNotification;

    private Color hubColor;
    private Dictionary<int, List<CharacterProfileSimple>> scenarioProfiles = new Dictionary<int, List<CharacterProfileSimple>>();
    private Dictionary<string, int> playerVotes = new Dictionary<string, int>();
    private float voteTimer = 30f;
    private bool isVoting = false;

    private void Start()
    {
        for (int i = 0; i < scenarioButtons.Length; i++)
        {
            int index = i;
            scenarioButtons[i].onClick.AddListener(() => OnScenarioSelected(index));
        }
        scenarioPanel.SetActive(false);
    }

    public void DisplayScenarios(List<string> scenarios)
    {
        hubColor = GameManager.Instance.GetCurrentHubColor();
        scenarioPanel.SetActive(true);
        
        backgroundImage.color = new Color(hubColor.r, hubColor.g, hubColor.b, 0.9f);

        for (int i = 0; i < scenarioTexts.Length; i++)
        {
            scenarioTexts[i].text = i < scenarios.Count ? scenarios[i] : "";
            scenarioButtons[i].gameObject.SetActive(i < scenarios.Count);
            
            Image panelImage = scenarioButtons[i].GetComponent<Image>();
            panelImage.color = i == 1 ? DarkenColor(hubColor, 0.2f) : hubColor;
            
            StartCoroutine(AnimatePanelIn(scenarioButtons[i].GetComponent<RectTransform>(), i));
        }
        
        StartCoroutine(AnimateScaleIn(whatIfText.transform));
        StartCoroutine(AnimateScaleIn(timerText.transform));
        
        StartVoting();
    }

    private IEnumerator AnimatePanelIn(RectTransform rectTransform, int index)
    {
        float startY = index == 1 ? -1000 : 1000;
        float endY = 0;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);
            float currentY = Mathf.Lerp(startY, endY, t);
            rectTransform.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector2(0, endY);
    }

    private IEnumerator AnimateScaleIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void StartVoting()
    {
        playerVotes.Clear();
        voteTimer = 30f;
        isVoting = true;
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SimulateAIVoting());
        }
    }

    private void OnScenarioSelected(int scenarioIndex)
    {
        if (!isVoting) return;

        string playerName = PhotonNetwork.LocalPlayer.NickName;
        photonView.RPC("RPC_SubmitVote", RpcTarget.All, scenarioIndex, playerName);
    }

    [PunRPC]
    private void RPC_SubmitVote(int scenarioIndex, string playerName)
    {
        if (!playerVotes.ContainsKey(playerName))
        {
            playerVotes[playerName] = scenarioIndex;
            DisplayVote(playerName, scenarioIndex);
        }
    }

    private void DisplayVote(string playerName, int scenarioIndex)
{
    UniversalCharacterController character = GameManager.Instance.GetCharacterByName(playerName);
    if (character != null)
    {
        CharacterProfileSimple profile = Instantiate(characterProfilePrefab, profileContainers[scenarioIndex]).GetComponent<CharacterProfileSimple>();
        profile.SetProfileInfo(playerName, character.GetCharacterColor(), !character.IsPlayerControlled, character.photonView.IsMine);
        
        StartCoroutine(AnimateScaleIn(profile.transform));

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
        // Hide scenario panel
        yield return StartCoroutine(HideScenarioPanel());

        // Implement the winning scenario
        GameManager.Instance.ImplementEmergentScenario(winningScenarioText);

        // Wait for notification to complete
        yield return new WaitForSeconds(emergentScenarioNotification.GetNotificationDuration());

        // Reset player positions
        GameManager.Instance.ResetPlayerPositions();

        // End emergent scenario state
        GameManager.Instance.EndEmergentScenario();
    }

    [PunRPC]
    private void RPC_EndEmergentScenario(string winningScenarioText)
    {
        StartCoroutine(EndEmergentScenarioSequence(winningScenarioText));
    }

    private IEnumerator EndEmergentScenarioSequence(string winningScenarioText)
    {
        // Hide scenario panel
        yield return StartCoroutine(HideScenarioPanel());

        // Show notification
        emergentScenarioNotification.DisplayNotification(winningScenarioText);
        yield return new WaitForSeconds(emergentScenarioNotification.GetNotificationDuration());

        // Update game state and respawn characters
        GameManager.Instance.UpdateGameState("SYSTEM", winningScenarioText, true);
        GameManager.Instance.ResetPlayerPositions();

        // End emergent scenario state
        GameManager.Instance.EndEmergentScenario();
    }

    private IEnumerator HideScenarioPanel()
    {
        for (int i = 0; i < scenarioButtons.Length; i++)
        {
            StartCoroutine(AnimatePanelOut(scenarioButtons[i].GetComponent<RectTransform>(), i));
        }
        
        StartCoroutine(AnimateScaleOut(whatIfText.transform));
        StartCoroutine(AnimateScaleOut(timerText.transform));
        
        yield return new WaitForSeconds(animationDuration);
        scenarioPanel.SetActive(false);
    }

    private IEnumerator AnimatePanelOut(RectTransform rectTransform, int index)
    {
        float startY = 0;
        float endY = index == 1 ? -1000 : 1000;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);
            float currentY = Mathf.Lerp(startY, endY, t);
            rectTransform.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector2(0, endY);
    }

    private IEnumerator AnimateScaleOut(Transform target)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);
            target.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }

        target.localScale = Vector3.zero;
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

    private Color DarkenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a
        );
    }
}