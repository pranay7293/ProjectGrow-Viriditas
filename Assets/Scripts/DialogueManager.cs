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
    [SerializeField] private TextMeshProUGUI dialogueText;
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

    // Chat Log Data Structures
    private Dictionary<string, List<ChatLogEntry>> chatLog = new Dictionary<string, List<ChatLogEntry>>();
    private List<string> characterList = new List<string>();
    private bool wasUIActiveLastFrame = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeUI()
    {
        // Initialize Dialogue Panel
        dialoguePanel.SetActive(false);
        loadingIndicator.SetActive(false);
        submitCustomInputButton.onClick.AddListener(SubmitCustomInput);
        endConversationButton.onClick.AddListener(EndConversation);

        customInputField.onValueChanged.AddListener(OnCustomInputValueChanged);
        customInputField.onEndEdit.AddListener(OnCustomInputEndEdit);

        // Initialize Chat Log Panel
        chatLogPanel.SetActive(false);
        if (characterFilter != null)
        {
            characterFilter.ClearOptions();
            characterFilter.AddOptions(new List<string> { "All Characters" });
            characterFilter.onValueChanged.AddListener(FilterChatLog);
        }

        // Initialize Option Buttons
        foreach (Button button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Initialize Chat Log Data Structures
        InitializeChatLog();
    }

    private void InitializeChatLog()
    {
        // Optionally, prepopulate with known characters or leave empty
        // Example:
        // characterList.Add("Player");
        // chatLog["Player"] = new List<ChatLogEntry>();
    }

    public async void InitiateDialogue(UniversalCharacterController npc)
    {
        if (npc == null || currentState != DialogueState.Idle)
        {
            return;
        }

        currentNPC = npc;
        currentNPC.AddState(UniversalCharacterController.CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        string initialDialogue = $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"Hello!\"";
        dialogueText.text = initialDialogue;
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        SetCustomInputActive(false);

        AddToChatLog(currentNPC.characterName, initialDialogue);

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

        string context = GetCurrentContext();
        currentOptions = await OpenAIService.Instance.GetGenerativeChoices(currentNPC.characterName, context, currentNPC.aiSettings);
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

                string colorHex = options[i].Category == DialogueCategory.Casual ? "#00BFFF" : "#FFD700";
                optionTexts[i].text = $"<color={colorHex}>[{categoryText}]</color> {optionText}";
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
            ProcessPlayerChoice(selectedOption, isNaturalDialogue: false);
            AddToChatLog("Player", $"<color=#FFD700>[{currentOptions[optionIndex].Category.ToString().ToUpper()}]</color> {selectedOption}");
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
        if (dialogInputWindow != null)
        {
            dialogInputWindow.SetActive(active);
        }
        else
        {
            Debug.LogWarning("DialogInputWindow is not assigned in the DialogueManager.");
        }

        if (active && customInputField != null)
        {
            customInputField.ActivateInputField();
        }
    }

    public void SubmitCustomInput()
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentNPC != null && !string.IsNullOrEmpty(customInputField.text))
        {
            string playerInput = customInputField.text;
            customInputField.text = "";
            SetCustomInputActive(false);
            ProcessPlayerChoice(playerInput, isNaturalDialogue: true);
            AddToChatLog("Player", $"<color=#00BFFF>[Casual]</color> {playerInput}");
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

    private async void ProcessPlayerChoice(string playerChoice, bool isNaturalDialogue)
    {
        if (currentState != DialogueState.WaitingForPlayerInput || isProcessingInput)
        {
            return;
        }

        SetDialogueState(DialogueState.ProcessingPlayerInput);
        isProcessingInput = true;
        ShowLoadingIndicator(true);

        string playerDialogue = $"You say to <color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color>: \"{playerChoice}\"";
        dialogueText.text = playerDialogue;
        AddToChatLog("Player", playerDialogue);
        GameManager.Instance.AddPlayerAction(playerChoice);

        string aiResponse;
        if (isNaturalDialogue)
        {
            aiResponse = await OpenAIService.Instance.GetNaturalDialogueResponse(currentNPC.characterName, playerChoice, currentNPC.aiSettings);
        }
        else
        {
            aiResponse = await OpenAIService.Instance.GetResponse(GetResponsePrompt(playerChoice), currentNPC.aiSettings);
        }

        string npcDialogue = $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"{aiResponse}\"";
        dialogueText.text = npcDialogue;
        AddToChatLog(currentNPC.characterName, npcDialogue);
        GameManager.Instance.UpdateGameState(currentNPC.characterName, aiResponse);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayChoices();

        SetDialogueState(DialogueState.WaitingForPlayerInput);
        isProcessingInput = false;
        ShowLoadingIndicator(false);
    }

    public void EndConversation()
    {
        if (currentState == DialogueState.Idle)
        {
            return;
        }

        if (currentNPC != null)
        {
            currentNPC.RemoveState(UniversalCharacterController.CharacterState.Chatting);
        }
        dialoguePanel.SetActive(false);
        SetCustomInputActive(false);
        currentNPC = null;
        customInputField.text = "";
        SetDialogueState(DialogueState.Idle);
        isProcessingInput = false;
        isGeneratingChoices = false;
        ShowLoadingIndicator(false);
    }

    private void ShowLoadingIndicator(bool show)
    {
        loadingIndicator.SetActive(show);
    }

    private void SetDialogueState(DialogueState newState)
    {
        currentState = newState;
    }

    private string GetCurrentContext()
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        List<string> recentEurekas = EurekaManager.Instance.GetRecentEurekas();
        string eurekaContext = recentEurekas.Count > 0 ? $"Recent breakthroughs: {string.Join(", ", recentEurekas)}" : "";
        return $"Current challenge: {currentState.CurrentChallenge.title}. Milestones: {string.Join(", ", currentState.CurrentChallenge.milestones)}. {eurekaContext}";
    }

    private string GetResponsePrompt(string playerInput)
    {
        return $"The player said: '{playerInput}'. Respond to this in the context of the current challenge: {GetCurrentContext()}";
    }

    // Chat Log Methods
    public void AddToChatLog(string speaker, string message)
    {
        if (!chatLog.ContainsKey(speaker))
        {
            chatLog[speaker] = new List<ChatLogEntry>();
            characterList.Add(speaker);
            UpdateCharacterFilter();
        }

        string timestamp = GetFormattedGameTime();
        ChatLogEntry entry = new ChatLogEntry
        {
            Timestamp = timestamp,
            Speaker = speaker,
            Message = message
        };
        chatLog[speaker].Add(entry);

        if (chatLog[speaker].Count > maxChatLogEntries)
        {
            chatLog[speaker].RemoveAt(0);
        }

        UpdateChatLogDisplay();
    }

    private string GetFormattedGameTime()
    {
        if (GameManager.Instance != null)
        {
            float totalSeconds = GameManager.Instance.GetChallengeDuration() - GameManager.Instance.GetRemainingTime();
            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
        else
        {
            return System.DateTime.Now.ToString("HH:mm:ss");
        }
    }

    private void UpdateChatLogDisplay()
    {
        if (chatLogScrollRect == null || !chatLogPanel.activeSelf)
        {
            return;
        }

        string selectedCharacter = characterFilter.options[characterFilter.value].text;
        List<ChatLogEntry> filteredLog;

        if (selectedCharacter == "All Characters")
        {
            filteredLog = chatLog.SelectMany(kv => kv.Value).OrderBy(entry => entry.Timestamp).ToList();
        }
        else
        {
            if (chatLog.ContainsKey(selectedCharacter))
            {
                filteredLog = chatLog[selectedCharacter].OrderBy(entry => entry.Timestamp).ToList();
            }
            else
            {
                filteredLog = new List<ChatLogEntry>();
            }
        }

        chatLogText.text = string.Join("\n", filteredLog.Select(entry => $"[{entry.Timestamp}] {entry.Message}"));

        Canvas.ForceUpdateCanvases();
        chatLogScrollRect.verticalNormalizedPosition = 0f;
    }

    private void UpdateCharacterFilter()
    {
        if (characterFilter == null) return;

        characterFilter.ClearOptions();
        List<string> options = new List<string> { "All Characters" };
        options.AddRange(characterList);
        characterFilter.AddOptions(options);
    }

    private void FilterChatLog(int index)
    {
        UpdateChatLogDisplay();
    }

    public void ToggleChatLog()
    {
        chatLogPanel.SetActive(!chatLogPanel.activeSelf);
        InputManager.Instance.SetUIActive(chatLogPanel.activeSelf);
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

    // Processing Natural Dialogues Initiated by AI Agents
    public async void InitiateNaturalDialogue(string aiCharacterName)
    {
        if (currentState != DialogueState.Idle)
        {
            return;
        }

        UniversalCharacterController aiNPC = GameManager.Instance.GetCharacterByName(aiCharacterName);
        if (aiNPC == null)
        {
            Debug.LogWarning($"AI character '{aiCharacterName}' not found.");
            return;
        }

        currentNPC = aiNPC;
        currentNPC.AddState(UniversalCharacterController.CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        // AI initiates the conversation
        string initialDialogue = await OpenAIService.Instance.GetNaturalDialogueResponse(currentNPC.characterName, "", currentNPC.aiSettings);
        dialogueText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"{initialDialogue}\"";
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        SetCustomInputActive(false);

        AddToChatLog(currentNPC.characterName, $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"{initialDialogue}\"");

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    // Handling Dialogue Requests from AI Agents
    public void HandleDialogueRequest(int aiCharacterViewID)
    {
        PhotonView aiView = PhotonView.Find(aiCharacterViewID);
        if (aiView == null)
        {
            Debug.LogWarning("AI character PhotonView not found for dialogue request.");
            return;
        }

        UniversalCharacterController aiNPC = aiView.GetComponent<UniversalCharacterController>();
        if (aiNPC == null)
        {
            Debug.LogWarning("UniversalCharacterController not found on AI character for dialogue request.");
            return;
        }

        // Show DialogueRequestUI to the player
        DialogueRequestUI.Instance.ShowRequest(aiNPC);
    }

    // Called when player accepts the dialogue request
    public async void AcceptDialogueRequest(UniversalCharacterController aiNPC)
    {
        if (aiNPC == null || currentState != DialogueState.Idle)
        {
            return;
        }

        currentNPC = aiNPC;
        currentNPC.AddState(UniversalCharacterController.CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        // AI initiates the conversation
        string initialDialogue = await OpenAIService.Instance.GetNaturalDialogueResponse(currentNPC.characterName, "", currentNPC.aiSettings);
        dialogueText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"{initialDialogue}\"";
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        SetCustomInputActive(false);

        AddToChatLog(currentNPC.characterName, $"<color=#{ColorUtility.ToHtmlStringRGB(currentNPC.characterColor)}>{currentNPC.characterName}</color> says to you: \"{initialDialogue}\"");

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    // Called when player declines the dialogue request
    public void DeclineDialogueRequest()
    {
        DialogueRequestUI.Instance.HideRequest();
    }

    // Helper Classes
    private class ChatLogEntry
    {
        public string Timestamp;
        public string Speaker;
        public string Message;
    }

    // New method to handle NPC-to-NPC dialogues
    public async void TriggerNPCDialogue(UniversalCharacterController initiator, UniversalCharacterController target)
    {
        if (initiator == null || target == null)
        {
            Debug.LogWarning("Invalid characters for NPC dialogue.");
            return;
        }

        string initialDialogue = await OpenAIService.Instance.GetNaturalDialogueResponse(initiator.characterName, $"Initiate a conversation with {target.characterName}", initiator.aiSettings);
        AddToChatLog(initiator.characterName, $"<color=#{ColorUtility.ToHtmlStringRGB(initiator.characterColor)}>{initiator.characterName}</color> says to {target.characterName}: \"{initialDialogue}\"");

        // Simulate a back-and-forth conversation
        for (int i = 0; i < 3; i++)
        {
            string response = await OpenAIService.Instance.GetNaturalDialogueResponse(target.characterName, initialDialogue, target.aiSettings);
            AddToChatLog(target.characterName, $"<color=#{ColorUtility.ToHtmlStringRGB(target.characterColor)}>{target.characterName}</color> responds: \"{response}\"");

            initialDialogue = await OpenAIService.Instance.GetNaturalDialogueResponse(initiator.characterName, response, initiator.aiSettings);
            AddToChatLog(initiator.characterName, $"<color=#{ColorUtility.ToHtmlStringRGB(initiator.characterColor)}>{initiator.characterName}</color> says: \"{initialDialogue}\"");
        }

        // End the conversation
        AddToChatLog("System", $"The conversation between {initiator.characterName} and {target.characterName} ends.");
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
    RiskTaking,
    Casual
}