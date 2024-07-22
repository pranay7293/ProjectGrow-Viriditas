using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DialogueManager : MonoBehaviourPunCallbacks
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue Panel")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private TMP_InputField customInputField;
    [SerializeField] private Button submitCustomInputButton;

    [Header("Chat Log")]
    [SerializeField] private GameObject chatLogPanel;
    [SerializeField] private TextMeshProUGUI chatLogText;
    [SerializeField] private TMP_Dropdown characterFilter;
    [SerializeField] private int maxChatLogEntries = 100;
    [SerializeField] private Button toggleChatLogButton;
    [SerializeField] private ScrollRect chatLogScrollRect;
    [SerializeField] private CanvasGroup chatLogCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    private UniversalCharacterController currentNPC;
    private Dictionary<string, List<string>> chatLog = new Dictionary<string, List<string>>();
    private List<string> characterList = new List<string>();

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
        dialoguePanel.SetActive(false);
        chatLogPanel.SetActive(false);
        submitCustomInputButton.onClick.AddListener(SubmitCustomInput);
        toggleChatLogButton.onClick.AddListener(ToggleChatLog);
        characterFilter.onValueChanged.AddListener(FilterChatLog);
        InitializeCharacterFilter();
    }

    private void InitializeCharacterFilter()
    {
        characterFilter.ClearOptions();
        characterFilter.AddOptions(new List<string> { "All Characters" });
    }

    public void InitiateDialogue(UniversalCharacterController npc)
    {
        if (npc == null)
        {
            Debug.LogError("Attempted to initiate dialogue with null NPC");
            return;
        }
        currentNPC = npc;
        InputManager.Instance.IsInDialogue = true;
        photonView.RPC("RPC_OpenDialogue", RpcTarget.All, npc.photonView.ViewID);
    }

    [PunRPC]
    private async void RPC_OpenDialogue(int npcViewID)
    {
        UniversalCharacterController npc = PhotonView.Find(npcViewID).GetComponent<UniversalCharacterController>();
        if (npc != null)
        {
            currentNPC = npc;
            npcNameText.text = npc.characterName;
            dialoguePanel.SetActive(true);
            string[] options = await npc.GetComponent<AIManager>().GetGenerativeChoices();

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Length)
                {
                    optionTexts[i].text = options[i];
                    int index = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectDialogueOption(index));
                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            customInputField.text = "";
            AddToChatLog(npc.characterName, "is ready to talk.");
        }
    }

    private void SelectDialogueOption(int optionIndex)
    {
        if (currentNPC != null && optionIndex >= 0 && optionIndex < optionTexts.Length)
        {
            string selectedOption = optionTexts[optionIndex].text;
            ProcessPlayerChoice(selectedOption);
        }
        else
        {
            Debug.LogError($"SelectDialogueOption: Invalid option index {optionIndex} or currentNPC is null");
            CloseDialogue();
        }
    }

    private void SubmitCustomInput()
    {
        if (currentNPC != null && !string.IsNullOrEmpty(customInputField.text))
        {
            ProcessPlayerChoice(customInputField.text);
        }
    }

    private async void ProcessPlayerChoice(string playerChoice)
    {
        if (currentNPC == null || GameManager.Instance == null)
        {
            Debug.LogError("ProcessPlayerChoice: currentNPC or GameManager.Instance is null");
            CloseDialogue();
            return;
        }

        AddToChatLog("Player", playerChoice);
        GameManager.Instance.AddPlayerAction(playerChoice);
        
        AIManager aiManager = currentNPC.GetComponent<AIManager>();
        if (aiManager == null)
        {
            Debug.LogError("ProcessPlayerChoice: AIManager not found on currentNPC");
            CloseDialogue();
            return;
        }

        string aiResponse = await aiManager.MakeDecision(playerChoice);
        AddToChatLog(currentNPC.characterName, aiResponse);
        GameManager.Instance.UpdateGameState(currentNPC.characterName, aiResponse);
        CloseDialogue();
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        InputManager.Instance.IsInDialogue = false;
        currentNPC = null;
    }

    public void AddToChatLog(string speaker, string message)
    {
        if (!chatLog.ContainsKey(speaker))
        {
            chatLog[speaker] = new List<string>();
            characterList.Add(speaker);
            UpdateCharacterFilter();
        }

        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {speaker}: {message}";
        chatLog[speaker].Add(logEntry);

        if (chatLog[speaker].Count > maxChatLogEntries)
        {
            chatLog[speaker].RemoveAt(0);
        }

        UpdateChatLogDisplay();
    }

    private void UpdateChatLogDisplay()
    {
        string selectedCharacter = characterFilter.options[characterFilter.value].text;
        List<string> filteredLog;

        if (selectedCharacter == "All Characters")
        {
            filteredLog = chatLog.SelectMany(kv => kv.Value).OrderBy(s => s).ToList();
        }
        else
        {
            filteredLog = chatLog[selectedCharacter];
        }

        chatLogText.text = string.Join("\n", filteredLog);
        
        Canvas.ForceUpdateCanvases();
        chatLogScrollRect.verticalNormalizedPosition = 0f;
    }

    private void UpdateCharacterFilter()
    {
        characterFilter.ClearOptions();
        characterFilter.AddOptions(new List<string> { "All Characters" });
        characterFilter.AddOptions(characterList);
    }

    private void FilterChatLog(int index)
    {
        UpdateChatLogDisplay();
    }

    public void ToggleChatLog()
    {
    if (chatLogPanel.activeSelf)
    {
        StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, chatLogCanvasGroup.alpha, 0f, fadeDuration));
    }
    else
    {
        chatLogPanel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, chatLogCanvasGroup.alpha, 1f, fadeDuration));
        UpdateChatLogDisplay();
    }
    InputManager.Instance.IsChatLogOpen = chatLogPanel.activeSelf;
    UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = end;
        if (end == 0f)
        {
            chatLogPanel.SetActive(false);
        }
    }

    public void LogAgentToAgentInteraction(string initiator, string target, string initiatorDialogue, string targetResponse)
    {
        AddToChatLog(initiator, initiatorDialogue);
        AddToChatLog(target, targetResponse);
    }

    public void TriggerNPCDialogue(UniversalCharacterController initiator, UniversalCharacterController target)
    {
        photonView.RPC("RPC_TriggerNPCDialogue", RpcTarget.All, initiator.photonView.ViewID, target.photonView.ViewID);
    }

    [PunRPC]
    private async void RPC_TriggerNPCDialogue(int initiatorViewID, int targetViewID)
    {
        UniversalCharacterController initiator = PhotonView.Find(initiatorViewID).GetComponent<UniversalCharacterController>();
        UniversalCharacterController target = PhotonView.Find(targetViewID).GetComponent<UniversalCharacterController>();

        if (initiator != null && target != null)
        {
            string initiatorDialogue = await initiator.GetComponent<AIManager>().GetNPCDialogue(target.characterName);
            string targetResponse = await target.GetComponent<AIManager>().GetNPCDialogue(initiator.characterName);

            LogAgentToAgentInteraction(initiator.characterName, target.characterName, initiatorDialogue, targetResponse);

            GameManager.Instance.UpdateGameState(initiator.characterName, initiatorDialogue);
            GameManager.Instance.UpdateGameState(target.characterName, targetResponse);
        }
    }
}