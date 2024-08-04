using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class EmergentScenarioUI : MonoBehaviourPunCallbacks
{
    public static EmergentScenarioUI Instance { get; private set; }

    [SerializeField] private GameObject scenarioPanel;
    [SerializeField] private TextMeshProUGUI scenarioDescriptionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI whatIfText;

    private Dictionary<int, int> votes = new Dictionary<int, int>();
    private float voteTimer = 30f;
    private bool isVoting = false;

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
        scenarioPanel.SetActive(false);
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

    public void DisplayScenario(EmergentScenarioGenerator.ScenarioData scenario, Color hubColor)
    {
        scenarioDescriptionText.text = scenario.description;
        SetupOptions(scenario.options);
        
        // Apply hub color to the background
        backgroundImage.color = hubColor;
        
        // Ensure border is visible
        borderImage.color = Color.white;
        
        // Adjust text color for better contrast
        Color textColor = GetContrastingTextColor(hubColor);
        scenarioDescriptionText.color = textColor;
        whatIfText.color = textColor;
        timerText.color = textColor;
        foreach (var optionText in optionTexts)
        {
            optionText.color = textColor;
        }

        scenarioPanel.SetActive(true);
        StartVoting(scenario.options);
    }

    private void SetupOptions(List<string> options)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = options[i];
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SubmitVote(index));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void StartVoting(List<string> options)
    {
        votes.Clear();
        voteTimer = 30f;
        isVoting = true;
    }

    private void SubmitVote(int optionIndex)
    {
        if (!isVoting) return;

        int playerID = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC("RPC_SubmitVote", RpcTarget.All, optionIndex, playerID);
    }

    [PunRPC]
    private void RPC_SubmitVote(int optionIndex, int playerID)
    {
        votes[playerID] = optionIndex;
        
        // If all players have voted, end voting immediately
        if (votes.Count == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            EndVoting();
        }
    }

    private void EndVoting()
    {
        isVoting = false;
        int winningOption = DetermineWinningOption();
        EmergentScenarioGenerator.Instance.ResolveScenario(optionTexts[winningOption].text);
        scenarioPanel.SetActive(false);
    }

    private int DetermineWinningOption()
    {
        if (votes.Count == 0) return 0;

        var voteCounts = new Dictionary<int, int>();
        foreach (var vote in votes.Values)
        {
            if (!voteCounts.ContainsKey(vote))
                voteCounts[vote] = 0;
            voteCounts[vote]++;
        }

        int maxVotes = voteCounts.Values.Max();
        var winningOptions = voteCounts.Where(kv => kv.Value == maxVotes)
                                       .Select(kv => kv.Key)
                                       .ToList();

        return winningOptions[Random.Range(0, winningOptions.Count)];
    }

    private Color GetContrastingTextColor(Color backgroundColor)
    {
        float brightness = (backgroundColor.r * 299 + backgroundColor.g * 587 + backgroundColor.b * 114) / 1000;
        return brightness > 0.5f ? Color.black : Color.white;
    }
}