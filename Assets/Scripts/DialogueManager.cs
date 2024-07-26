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
    [SerializeField] private TextMeshProUGUI npcResponseText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private TMP_InputField customInputField;
    [SerializeField] private Button submitCustomInputButton;
    [SerializeField] private Button endConversationButton;
    [SerializeField] private GameObject loadingIndicator;

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
    private List<string> currentOptions = new List<string>();
    private bool isProcessingInput = false;
    private bool isGeneratingChoices = false;

    private enum DialogueState
    {
        Idle,
        WaitingForPlayerInput,
        ProcessingPlayerInput,
        GeneratingResponse
    }

    private DialogueState currentState = DialogueState.Idle;

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
        InitializeUI();
    }

    private void InitializeUI()
    {
        dialoguePanel.SetActive(false);
        chatLogPanel.SetActive(false);
        loadingIndicator.SetActive(false);
        submitCustomInputButton.onClick.AddListener(SubmitCustomInput);
        toggleChatLogButton.onClick.AddListener(ToggleChatLog);
        endConversationButton.onClick.AddListener(EndConversation);
        characterFilter.onValueChanged.AddListener(FilterChatLog);
        
        customInputField.onValueChanged.RemoveAllListeners();
        customInputField.onEndEdit.RemoveAllListeners();
        customInputField.onEndEdit.AddListener(OnCustomInputEndEdit);
        
        InitializeCharacterFilter();
    }

    private void InitializeCharacterFilter()
    {
        characterFilter.ClearOptions();
        characterFilter.AddOptions(new List<string> { "All Characters" });
        characterFilter.AddOptions(CharacterSelectionManager.characterFullNames.ToList());
    }

    public async void InitiateDialogue(UniversalCharacterController npc)
    {
        if (npc == null || currentState != DialogueState.Idle)
        {
            return;
        }

        currentNPC = npc;
        currentNPC.SetState(UniversalCharacterController.CharacterState.Interacting);
        InputManager.Instance.StartDialogue();
        
        npcNameText.text = npc.characterName;
        npcResponseText.text = $"Hello, I'm {npc.characterName}. How can I help you?";
        dialoguePanel.SetActive(true);
        customInputField.text = "";
        
        AddToChatLog(npc.characterName, npcResponseText.text);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    private async Task GenerateAndDisplayChoices()
    {
        if (isGeneratingChoices)
        {
            return;
        }

        isGeneratingChoices = true;
        ShowLoadingIndicator(true);

        currentOptions = await OpenAIService.Instance.GetGenerativeChoices(currentNPC.characterName, GetCurrentContext(), currentNPC.aiSettings);
        UpdateDialogueOptions(currentOptions);

        ShowLoadingIndicator(false);
        isGeneratingChoices = false;
    }

    private void UpdateDialogueOptions(List<string> options)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < options.Count)
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
    }

    private void SelectDialogueOption(int optionIndex)
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentNPC != null && optionIndex >= 0 && optionIndex < currentOptions.Count)
        {
            string selectedOption = currentOptions[optionIndex];
            ProcessPlayerChoice(selectedOption);
        }
        else
        {
            EndConversation();
        }
    }

    public void SubmitCustomInput()
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentNPC != null && !string.IsNullOrEmpty(customInputField.text))
        {
            ProcessPlayerChoice(customInputField.text);
        }
    }

    public void OnCustomInputEndEdit(string newValue)
    {
        submitCustomInputButton.interactable = !string.IsNullOrEmpty(newValue);
    }

    private async void ProcessPlayerChoice(string playerChoice)
    {
        if (currentState != DialogueState.WaitingForPlayerInput || isProcessingInput)
        {
            return;
        }

        SetDialogueState(DialogueState.ProcessingPlayerInput);
        isProcessingInput = true;
        ShowLoadingIndicator(true);

        AddToChatLog("Player", playerChoice);
        GameManager.Instance.AddPlayerAction(playerChoice);
        
        string aiResponse = await OpenAIService.Instance.GetResponse(GetResponsePrompt(playerChoice), currentNPC.aiSettings);
        npcResponseText.text = aiResponse;
        AddToChatLog(currentNPC.characterName, aiResponse);
        GameManager.Instance.UpdateGameState(currentNPC.characterName, aiResponse);

        customInputField.text = "";

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayChoices();

        SetDialogueState(DialogueState.WaitingForPlayerInput);
        isProcessingInput = false;
        ShowLoadingIndicator(false);
    }

    public void EndConversation()
    {
        if (currentNPC != null)
        {
            currentNPC.SetState(UniversalCharacterController.CharacterState.Idle);
        }
        dialoguePanel.SetActive(false);
        currentNPC = null;
        customInputField.text = "";
        SetDialogueState(DialogueState.Idle);
        isProcessingInput = false;
        isGeneratingChoices = false;
        ShowLoadingIndicator(false);
        InputManager.Instance.EndDialogue();
    }

    private void ShowLoadingIndicator(bool show)
    {
        loadingIndicator.SetActive(show);
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

    private string GetCurrentContext()
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        return $"Current challenge: {currentState.CurrentChallenge}. Subgoals: {string.Join(", ", currentState.CurrentSubgoals)}";
    }

    private string GetResponsePrompt(string playerInput)
    {
        return $"The player said: '{playerInput}'. Respond to this in the context of the current challenge: {GetCurrentContext()}";
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
            string initiatorDialogue = await OpenAIService.Instance.GetResponse(GetNPCDialoguePrompt(initiator, target), initiator.aiSettings);
            string targetResponse = await OpenAIService.Instance.GetResponse(GetNPCDialoguePrompt(target, initiator), target.aiSettings);

            AddToChatLog(initiator.characterName, initiatorDialogue);
            AddToChatLog(target.characterName, targetResponse);

            GameManager.Instance.UpdateGameState(initiator.characterName, initiatorDialogue);
            GameManager.Instance.UpdateGameState(target.characterName, targetResponse);
        }
    }

    private string GetNPCDialoguePrompt(UniversalCharacterController speaker, UniversalCharacterController listener)
    {
        return $"You are speaking to {listener.characterName}. Initiate a brief conversation related to the current context: {GetCurrentContext()}";
    }

    private void SetDialogueState(DialogueState newState)
    {
        currentState = newState;
    }
}