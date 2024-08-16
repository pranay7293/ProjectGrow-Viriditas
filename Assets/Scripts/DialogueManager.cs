using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
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
    [SerializeField] private GameObject dialogInputWindow;
    [SerializeField] private TMP_InputField customInputField;
    [SerializeField] private Button submitCustomInputButton;
    [SerializeField] private Button endConversationButton;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Chat Log")]
    [SerializeField] private GameObject chatLogPanel;
    [SerializeField] private TextMeshProUGUI chatLogText;
    [SerializeField] private TMP_Dropdown characterFilter;
    [SerializeField] private int maxChatLogEntries = 100;
    [SerializeField] private ScrollRect chatLogScrollRect;
    [SerializeField] private CanvasGroup chatLogCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    private UniversalCharacterController currentNPC;
    private Dictionary<string, List<string>> chatLog = new Dictionary<string, List<string>>();
    private List<string> characterList = new List<string>();
    private List<DialogueOption> currentOptions = new List<DialogueOption>();
    private bool isProcessingInput = false;
    private bool isGeneratingChoices = false;
    private bool isCustomInputActive = false;

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
    if (dialoguePanel != null) dialoguePanel.SetActive(false);
    if (chatLogPanel != null) chatLogPanel.SetActive(false);
    if (loadingIndicator != null) loadingIndicator.SetActive(false);
    if (submitCustomInputButton != null) submitCustomInputButton.onClick.AddListener(SubmitCustomInput);
    if (endConversationButton != null) endConversationButton.onClick.AddListener(EndConversation);
    
    if (customInputField != null)
    {
        customInputField.onValueChanged.AddListener(OnCustomInputValueChanged);
        customInputField.onEndEdit.AddListener(OnCustomInputEndEdit);
    }
    
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

        SetCustomInputActive(false);
        
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

    private void UpdateDialogueOptions(List<DialogueOption> options)
    {
        int optionsCount = Mathf.Max(options.Count, 3);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < optionsCount && optionButtons[i] != null)
            {
                string categoryText = i < options.Count ? options[i].Category.ToString().ToUpper() : "DEFAULT";
                string optionText = i < options.Count ? options[i].Text : $"Default option {i + 1}";
                
                optionTexts[i].text = $"<color=#FFD700>[{categoryText}]</color> {optionText}";
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectDialogueOption(index));
                optionButtons[i].gameObject.SetActive(true);
            }
            else if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectDialogueOption(int optionIndex)
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentNPC != null && optionIndex >= 0 && optionIndex < currentOptions.Count)
        {
            string selectedOption = currentOptions[optionIndex].Text;
            ProcessPlayerChoice(selectedOption);
        }
        else
        {
            EndConversation();
        }
    }

    public void ToggleCustomInput()
    {
        SetCustomInputActive(!isCustomInputActive);
    }

    private void SetCustomInputActive(bool active)
    {
        isCustomInputActive = active;
        dialogInputWindow.SetActive(active);

        if (active)
        {
            customInputField.ActivateInputField();
        }
    }

    public void SubmitCustomInput()
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentNPC != null && !string.IsNullOrEmpty(customInputField.text))
        {
            ProcessPlayerChoice(customInputField.text);
            SetCustomInputActive(false);
        }
    }

    private void OnCustomInputValueChanged(string newValue)
    {
        submitCustomInputButton.interactable = !string.IsNullOrEmpty(newValue);
    }

    private void OnCustomInputEndEdit(string newValue)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitCustomInput();
        }
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
        SetCustomInputActive(false);
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
        if (chatLogScrollRect == null || !chatLogPanel.activeSelf)
        {
            return;
        }

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
        chatLogPanel.SetActive(!chatLogPanel.activeSelf);
        if (chatLogPanel.activeSelf)
        {
            UpdateChatLogDisplay();
            StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, chatLogCanvasGroup.alpha, 1f, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, chatLogCanvasGroup.alpha, 0f, fadeDuration));
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    public void GenerateEurekaFromDialogue(string player1, string player2)
    {
        GameManager.Instance.GenerateEureka(player1, player2);
        // Update UI to show eureka generation
    }

    private string GetCurrentContext()
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        return $"Current challenge: {currentState.CurrentChallenge.title}. Milestones: {string.Join(", ", currentState.CurrentChallenge.milestones)}";
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
    PhotonView initiatorView = PhotonView.Find(initiatorViewID);
    PhotonView targetView = PhotonView.Find(targetViewID);

    if (initiatorView == null || targetView == null)
    {
        Debug.LogWarning("One or both characters have been destroyed. Skipping NPC dialogue.");
        return;
    }

    UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
    UniversalCharacterController target = targetView.GetComponent<UniversalCharacterController>();

    if (initiator != null && target != null)
    {
        string initiatorDialogue = await OpenAIService.Instance.GetResponse(GetNPCDialoguePrompt(initiator, target), initiator.aiSettings);
        string targetResponse = await OpenAIService.Instance.GetResponse(GetNPCDialoguePrompt(target, initiator), target.aiSettings);

        AddToChatLog(initiator.characterName, initiatorDialogue);
        AddToChatLog(target.characterName, targetResponse);

        GameManager.Instance.UpdateGameState(initiator.characterName, initiatorDialogue);
        GameManager.Instance.UpdateGameState(target.characterName, targetResponse);

        UpdateRelationshipAfterInteraction(initiator, target, initiatorDialogue, targetResponse);
    }
    }

    private string GetNPCDialoguePrompt(UniversalCharacterController speaker, UniversalCharacterController listener)
    {
        return $"You are {speaker.characterName} speaking to {listener.characterName}. Initiate a brief conversation related to the current context: {GetCurrentContext()}";
    }

    private void UpdateRelationshipAfterInteraction(UniversalCharacterController initiator, UniversalCharacterController target, string initiatorDialogue, string targetResponse)
    {
    if (initiator == null || target == null)
    {
        Debug.LogWarning("One or both characters have been destroyed. Skipping relationship update.");
        return;
    }

    float relationshipChange = CalculateRelationshipChange(initiatorDialogue, targetResponse);

    AIManager initiatorAI = initiator.GetComponent<AIManager>();
    AIManager targetAI = target.GetComponent<AIManager>();

    if (initiatorAI != null && targetAI != null)
    {
        initiatorAI.UpdateRelationship(target.characterName, relationshipChange);
        targetAI.UpdateRelationship(initiator.characterName, relationshipChange);
    }
    }

    private float CalculateRelationshipChange(string dialogue1, string dialogue2)
    {
        float change = 0f;
        string combinedDialogue = dialogue1.ToLower() + " " + dialogue2.ToLower();

        if (combinedDialogue.Contains("agree") || combinedDialogue.Contains("support") || combinedDialogue.Contains("like"))
        {
            change += 0.1f;
        }

        if (combinedDialogue.Contains("disagree") || combinedDialogue.Contains("oppose") || combinedDialogue.Contains("dislike"))
        {
            change -= 0.1f;
        }

        return Mathf.Clamp(change, -0.2f, 0.2f);
    }

    private void SetDialogueState(DialogueState newState)
    {
        currentState = newState;
    }
}

public class DialogueOption
{
    public string Text { get; set; }
    public DialogueCategory Category { get; set; }

    public DialogueOption(string text, DialogueCategory category)
    {
        Text = text;
        Category = category;
    }
}

public enum DialogueCategory
{
    Ethical,
    Strategic,
    Emotional,
    Practical,
    Creative,
    Diplomatic,
    RiskTaking
}