using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

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
    [SerializeField] private float profileDisplayDelay = 0.5f;

    private Dictionary<int, List<CharacterProfileSimple>> scenarioProfiles = new Dictionary<int, List<CharacterProfileSimple>>();
    private Dictionary<string, int> playerVotes = new Dictionary<string, int>();
    private List<string> pendingVotes = new List<string>();
    private float voteTimer = 30f;
    private bool isVoting = false;
    private int totalVotes = 0;

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
        
        scenarioPanel.SetActive(false);
    }

    private void Start()
    {
        for (int i = 0; i < scenarioButtons.Length; i++)
        {
            int index = i;
            scenarioButtons[i].onClick.AddListener(() => OnScenarioSelected(index));
        }
    }

    private void Update()
    {
<<<<<<< Updated upstream
        if (isVoting)
        {
            voteTimer -= Time.deltaTime;
            timerText.text = $"{Mathf.CeilToInt(voteTimer)}";

            if (voteTimer <= 0 || totalVotes >= 10)
            {
                EndVoting();
            }
=======
    if (IsVoting)
    {
        voteTimer -= Time.deltaTime;
        timerText.text = $"{Mathf.CeilToInt(voteTimer)}";

        if (voteTimer <= 0)
        {
            EndVoting();
>>>>>>> Stashed changes
        }
    }
    }

    public void DisplayScenarios(List<string> scenarios)
    {
        scenarioPanel.SetActive(true);
        for (int i = 0; i < scenarioTexts.Length; i++)
        {
            scenarioTexts[i].text = i < scenarios.Count ? scenarios[i] : "";
            scenarioButtons[i].gameObject.SetActive(i < scenarios.Count);
        }
        StartVoting();
    }

    private void StartVoting()
    {
        playerVotes.Clear();
        pendingVotes.Clear();
        totalVotes = 0;
        for (int i = 0; i < profileContainers.Length; i++)
        {
            scenarioProfiles[i] = new List<CharacterProfileSimple>();
        }
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
        if (!playerVotes.ContainsKey(playerName) && !pendingVotes.Contains(playerName))
        {
            pendingVotes.Add(playerName);
            totalVotes++;
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DisplayVoteWithDelay(playerName, scenarioIndex));
            }
        }
    }

    private IEnumerator DisplayVoteWithDelay(string playerName, int scenarioIndex)
    {
        yield return new WaitForSeconds(profileDisplayDelay);
        photonView.RPC("RPC_DisplayVote", RpcTarget.All, playerName, scenarioIndex);
    }

    [PunRPC]
    private void RPC_DisplayVote(string playerName, int scenarioIndex)
    {
        playerVotes[playerName] = scenarioIndex;
        pendingVotes.Remove(playerName);
        DisplayVote(playerName, scenarioIndex);
    }

    private void DisplayVote(string playerName, int scenarioIndex)
    {
        UniversalCharacterController character = GameManager.Instance.GetCharacterByName(playerName);
        if (character != null)
        {
            CharacterProfileSimple profile = Instantiate(characterProfilePrefab, profileContainers[scenarioIndex]).GetComponent<CharacterProfileSimple>();
            profile.SetProfileInfo(playerName, character.characterColor, !character.IsPlayerControlled, character.photonView.IsMine);
            scenarioProfiles[scenarioIndex].Add(profile);
        }
    }

    private IEnumerator SimulateAIVoting()
    {
        List<string> aiCharacters = GameManager.Instance.GetAICharacterNames();
        float timePerVote = (voteTimer - 5f) / aiCharacters.Count; // Reserve 5 seconds at the end

        foreach (string aiName in aiCharacters)
        {
            yield return new WaitForSeconds(Random.Range(0f, timePerVote));
            if (isVoting && !playerVotes.ContainsKey(aiName) && !pendingVotes.Contains(aiName))
            {
                AIManager aiManager = GameManager.Instance.GetCharacterByName(aiName).GetComponent<AIManager>();
                List<string> scenarioDescriptions = GetCurrentScenarios();
                GameState currentGameState = GameManager.Instance.GetCurrentGameState();
                int chosenScenario = aiManager.DecideScenario(scenarioDescriptions, currentGameState);
                photonView.RPC("RPC_SubmitVote", RpcTarget.All, chosenScenario, aiName);
            }
        }
    }

    private void EndVoting()
    {
        isVoting = false;
        int winningScenario = DetermineWinningScenario();
        GameManager.Instance.UpdateGameState("SYSTEM", scenarioTexts[winningScenario].text, true);
        StartCoroutine(HideScenarioPanelWithDelay());
    }

    private IEnumerator HideScenarioPanelWithDelay()
    {
        yield return new WaitForSeconds(2f); // Give players time to see the final result
        scenarioPanel.SetActive(false);
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
        var winningScenarios = voteCounts.Where(kv => kv.Value == maxVotes)
                                         .Select(kv => kv.Key)
                                         .ToList();

        return winningScenarios[Random.Range(0, winningScenarios.Count)];
    }

    private List<string> GetCurrentScenarios()
    {
        return scenarioTexts.Select(text => text.text).ToList();
    }
}