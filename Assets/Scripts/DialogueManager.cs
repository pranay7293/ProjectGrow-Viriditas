using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using static CharacterState;

public class DialogueManager : MonoBehaviourPunCallbacks
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue Panel")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI agentDialogueText;
    [SerializeField] private Button[] generativeChoiceButtons;
    [SerializeField] private TextMeshProUGUI[] generativeChoiceTexts;
    [SerializeField] private DialogueDisplayManager dialogueDisplayManager;
    [SerializeField] private GameObject customInputWindow;
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

    private UniversalCharacterController currentAgent;
    private List<GenerativeChoiceOption> currentChoices = new List<GenerativeChoiceOption>();
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
    private Dictionary<string, List<ChatLogEntry>> chatLog = new Dictionary<string, List<ChatLogEntry>>();
    private List<string> characterList = new List<string>();
    private bool wasUIActiveLastFrame = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("DialogueManager: DialoguePanel is not assigned.");
            return;
        }

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (submitCustomInputButton != null)
            submitCustomInputButton.onClick.AddListener(SubmitCustomInput);

        if (endConversationButton != null)
            endConversationButton.onClick.AddListener(EndConversation);

        if (customInputField != null)
        {
            customInputField.onValueChanged.AddListener(OnCustomInputValueChanged);
            customInputField.onEndEdit.AddListener(OnCustomInputEndEdit);
        }

        if (chatLogPanel != null)
        {
            chatLogPanel.SetActive(false);
            if (characterFilter != null)
            {
                characterFilter.ClearOptions();
                characterFilter.AddOptions(new List<string> { "All Characters" });
                characterFilter.onValueChanged.AddListener(FilterChatLog);
            }
        }

        if (generativeChoiceButtons != null && generativeChoiceTexts != null)
        {
            for (int i = 0; i < generativeChoiceButtons.Length; i++)
            {
                if (generativeChoiceButtons[i] != null)
                {
                    generativeChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("DialogueManager: GenerativeChoiceButtons or GenerativeChoiceTexts are not assigned.");
        }

        InitializeChatLog();
    }

    private void InitializeChatLog()
    {
        chatLog.Clear();
        characterList.Clear();
    }

    public async void InitiateDialogue(UniversalCharacterController agent)
    {
        if (agent == null || currentState != DialogueState.Idle)
            return;

        currentAgent = agent;
        currentAgent.AddState(CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        // Get conversation history and relevant memories
        var conversationHistory = currentAgent.aiManager.npcData.GetMentalModel().MemoryStream
            .GetConversationHistory("Player");
        string interactionHistory = string.Join("; ", conversationHistory.Select(m => m.Content));

        // Generate greeting using conversation context
        string greeting = await OpenAIService.Instance.GenerateAgentGreeting(
            currentAgent.characterName, 
            currentAgent.aiSettings, 
            interactionHistory
        );

        string initialDialogue = $"<color=#{ColorUtility.ToHtmlStringRGB(currentAgent.characterColor)}>{currentAgent.characterName}</color> says to you: \"{greeting}\"";
        agentDialogueText.text = initialDialogue;
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        // Record the interaction in memory stream
        currentAgent.aiManager.npcData.GetMentalModel().AddConversationMemory(
            currentAgent.characterName,
            greeting,
            "Player",
            0.7f
        );

        DialogueDisplayManager.Instance.ShowDialoguePrompt();
        SetCustomInputActive(false);

        AddToChatLog(currentAgent.characterName, initialDialogue);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayGenerativeChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    private async Task GenerateAndDisplayGenerativeChoices()
    {
        if (isGeneratingChoices)
            return;

        isGeneratingChoices = true;
        ShowLoadingIndicator(true);

        string context = GetCurrentContext();
        currentChoices = await OpenAIService.Instance.GetGenerativeChoices(
            currentAgent.characterName, 
            context, 
            currentAgent.aiSettings
        );

        if (currentChoices == null || currentChoices.Count == 0)
        {
            Debug.LogWarning("DialogueManager: No generative choices generated.");
            currentChoices = new List<GenerativeChoiceOption>
            {
                new GenerativeChoiceOption("I need to think more about this.", GenerativeChoiceCategory.Practical)
            };
        }

        UpdateGenerativeChoiceButtons(currentChoices);

        ShowLoadingIndicator(false);
        isGeneratingChoices = false;
    }

    private void UpdateGenerativeChoiceButtons(List<GenerativeChoiceOption> choices)
    {
        int optionsCount = Mathf.Min(choices.Count, generativeChoiceButtons.Length);
        for (int i = 0; i < generativeChoiceButtons.Length; i++)
        {
            if (i < optionsCount && generativeChoiceButtons[i] != null && generativeChoiceTexts[i] != null)
            {
                string categoryText = choices[i].Category.ToString().ToUpper();
                string optionText = choices[i].Text;

                string colorHex = "#FFD700";
                generativeChoiceTexts[i].text = $"<color={colorHex}>[{categoryText}]</color> {optionText}";
                int index = i;
                generativeChoiceButtons[i].onClick.RemoveAllListeners();
                generativeChoiceButtons[i].onClick.AddListener(() => SelectGenerativeChoiceOption(index));
                generativeChoiceButtons[i].gameObject.SetActive(true);
            }
            else if (generativeChoiceButtons[i] != null)
            {
                generativeChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectGenerativeChoiceOption(int optionIndex)
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentAgent != null && optionIndex >= 0 && optionIndex < currentChoices.Count)
        {
            string selectedOption = currentChoices[optionIndex].Text;
            ProcessPlayerChoice(selectedOption, isNaturalDialogue: false);
            AddToChatLog("Player", $"<color=#FFD700>[{currentChoices[optionIndex].Category.ToString().ToUpper()}]</color> {selectedOption}");
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
        if (customInputWindow != null)
        {
            if (active)
            {
                DialogueDisplayManager.Instance.ShowPlayerInput();
            }
            else
            {
                DialogueDisplayManager.Instance.ShowDialoguePrompt();
            }
            customInputWindow.SetActive(active);
        }
        else
        {
            Debug.LogWarning("DialogueManager: CustomInputWindow is not assigned.");
        }

        if (active && customInputField != null)
        {
            customInputField.ActivateInputField();
        }
    }

    public void SubmitCustomInput()
    {
        if (currentState != DialogueState.WaitingForPlayerInput) return;

        if (currentAgent != null && !string.IsNullOrEmpty(customInputField.text))
        {
            string playerInput = customInputField.text;
            customInputField.text = "";
            SetCustomInputActive(false);
            ProcessPlayerChoice(playerInput, isNaturalDialogue: true);
            AddToChatLog("Player", $"You: {playerInput}");
        }
    }

    private void OnCustomInputValueChanged(string newValue)
    {
        if (submitCustomInputButton != null)
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
            return;

        SetDialogueState(DialogueState.ProcessingPlayerInput);
        isProcessingInput = true;
        ShowLoadingIndicator(true);

        string playerDialogue = $"You say to <color=#{ColorUtility.ToHtmlStringRGB(currentAgent.characterColor)}>{currentAgent.characterName}</color>: \"{playerChoice}\"";
        agentDialogueText.text = playerDialogue;
        AddToChatLog("Player", playerDialogue);
        GameManager.Instance.AddPlayerAction(playerChoice);

        // Record player's input in memory stream
        currentAgent.aiManager.npcData.GetMentalModel().AddConversationMemory(
            "Player",
            playerChoice,
            currentAgent.characterName,
            0.6f
        );

        // Get relevant conversation history and memories
        var relevantMemories = currentAgent.aiManager.npcData.GetMentalModel().MemoryStream
            .RetrieveRelevantMemories(playerChoice);
        string memoryContext = string.Join("\n", relevantMemories.Select(m => m.Content));
        string reflection = currentAgent.aiManager.npcData.GetMentalModel().Reflect();

        // Generate agent response
        string agentResponse = await (isNaturalDialogue ?
            OpenAIService.Instance.GetAgentResponse(
                currentAgent.characterName,
                playerChoice,
                currentAgent.aiSettings,
                memoryContext,
                reflection
            ) :
            OpenAIService.Instance.GetAgentResponseToChoice(
                currentAgent.characterName,
                playerChoice,
                currentAgent.aiSettings,
                memoryContext,
                reflection
            ));

        // Record agent's response in memory stream
        currentAgent.aiManager.npcData.GetMentalModel().AddConversationMemory(
            currentAgent.characterName,
            agentResponse,
            "Player",
            0.7f
        );

        string agentDialogue = $"<color=#{ColorUtility.ToHtmlStringRGB(currentAgent.characterColor)}>{currentAgent.characterName}</color> says to you: \"{agentResponse}\"";
        agentDialogueText.text = agentDialogue;
        AddToChatLog(currentAgent.characterName, agentDialogue);
        GameManager.Instance.UpdateGameState(currentAgent.characterName, agentResponse);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayGenerativeChoices();

        SetDialogueState(DialogueState.WaitingForPlayerInput);
        isProcessingInput = false;
        ShowLoadingIndicator(false);
    }

    public void EndConversation()
    {
        if (currentState == DialogueState.Idle)
            return;

        if (currentAgent != null)
        {
            currentAgent.RemoveState(CharacterState.Chatting);
        }
        dialoguePanel.SetActive(false);
        SetCustomInputActive(false);
        currentAgent = null;
        customInputField.text = "";
        SetDialogueState(DialogueState.Idle);
        isProcessingInput = false;
        isGeneratingChoices = false;
        ShowLoadingIndicator(false);
        InputManager.Instance.EndDialogue();
    }

    private void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
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
        string eurekaContext = recentEurekas.Count > 0 ? 
            $"Recent breakthroughs: {string.Join(", ", recentEurekas)}" : "";
        
        return $"Current challenge: {currentState.CurrentChallenge.title}. " +
               $"Milestones: {string.Join(", ", currentState.CurrentChallenge.milestones)}. " +
               eurekaContext;
    }

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
        return System.DateTime.Now.ToString("HH:mm:ss");
    }

    private void UpdateChatLogDisplay()
    {
        if (chatLogScrollRect == null || !chatLogPanel.activeSelf)
            return;

        string selectedCharacter = characterFilter.options[characterFilter.value].text;
        List<ChatLogEntry> filteredLog;

        if (selectedCharacter == "All Characters")
        {
            filteredLog = chatLog.SelectMany(kv => kv.Value)
                               .OrderBy(entry => entry.Timestamp)
                               .ToList();
        }
        else
        {
            filteredLog = chatLog.ContainsKey(selectedCharacter) ?
                chatLog[selectedCharacter].OrderBy(entry => entry.Timestamp).ToList() :
                new List<ChatLogEntry>();
        }

        chatLogText.text = string.Join("\n", 
            filteredLog.Select(entry => $"[{entry.Timestamp}] {entry.Message}"));

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
        if (chatLogCanvasGroup == null)
        {
            Debug.LogError("DialogueManager: ChatLogCanvasGroup is not assigned.");
            return;
        }

        chatLogPanel.SetActive(!chatLogPanel.activeSelf);
        InputManager.Instance.SetUIActive(chatLogPanel.activeSelf);
        
        if (chatLogPanel.activeSelf)
        {
            UpdateChatLogDisplay();
            StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, 
                chatLogCanvasGroup.alpha, 1f, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeCanvasGroup(chatLogCanvasGroup, 
                chatLogCanvasGroup.alpha, 0f, fadeDuration));
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

    public async void InitiateAgentDialogue(string agentName)
    {
        if (currentState != DialogueState.Idle)
            return;

        UniversalCharacterController agentNPC = GameManager.Instance.GetCharacterByName(agentName);
        if (agentNPC == null)
        {
            Debug.LogWarning($"Agent character '{agentName}' not found.");
            return;
        }

        currentAgent = agentNPC;
        currentAgent.AddState(CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        // Get conversation history and memories
        var conversationHistory = agentNPC.aiManager.npcData.GetMentalModel().MemoryStream
            .GetConversationHistory("Player");
        string memoryContext = string.Join("\n", 
            conversationHistory.Select(m => m.Content));
        string reflection = agentNPC.aiManager.npcData.GetMentalModel().Reflect();

        string initialDialogue = await OpenAIService.Instance.GetAgentResponse(
            agentNPC.characterName,
            "",
            agentNPC.aiSettings,
            memoryContext,
            reflection
        );

        agentDialogueText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(agentNPC.characterColor)}>{agentNPC.characterName}</color> says to you: \"{initialDialogue}\"";
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        // Record the interaction
        agentNPC.aiManager.npcData.GetMentalModel().AddConversationMemory(
            agentNPC.characterName,
            initialDialogue,
            "Player",
            0.7f
        );

        SetCustomInputActive(false);
        AddToChatLog(agentNPC.characterName, agentDialogueText.text);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayGenerativeChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    public void HandleDialogueRequest(int agentViewID)
    {
        PhotonView agentView = PhotonView.Find(agentViewID);
        if (agentView == null)
        {
            Debug.LogWarning("DialogueManager: Agent PhotonView not found for dialogue request.");
            return;
        }

        UniversalCharacterController agentNPC = agentView.GetComponent<UniversalCharacterController>();
        if (agentNPC == null)
        {
            Debug.LogWarning("DialogueManager: UniversalCharacterController not found on agent for dialogue request.");
            return;
        }

        DialogueRequestUI.Instance.ShowRequest(agentNPC);
    }

    public async void AcceptDialogueRequest(UniversalCharacterController agentNPC)
    {
        if (agentNPC == null || currentState != DialogueState.Idle)
            return;

        currentAgent = agentNPC;
        currentAgent.AddState(CharacterState.Chatting);
        InputManager.Instance.StartDialogue();

        // Get conversation history and memories
        var conversationHistory = agentNPC.aiManager.npcData.GetMentalModel().MemoryStream
            .GetConversationHistory("Player");
        string memoryContext = string.Join("\n", 
            conversationHistory.Select(m => m.Content));
        string reflection = agentNPC.aiManager.npcData.GetMentalModel().Reflect();

        string initialDialogue = await OpenAIService.Instance.GetAgentResponse(
            agentNPC.characterName,
            "",
            agentNPC.aiSettings,
            memoryContext,
            reflection
        );

        agentDialogueText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(agentNPC.characterColor)}>{agentNPC.characterName}</color> says to you: \"{initialDialogue}\"";
        dialoguePanel.SetActive(true);
        customInputField.text = "";

        // Record the interaction
        agentNPC.aiManager.npcData.GetMentalModel().AddConversationMemory(
            agentNPC.characterName,
            initialDialogue,
            "Player",
            0.7f
        );

        SetCustomInputActive(false);
        AddToChatLog(agentNPC.characterName, agentDialogueText.text);

        SetDialogueState(DialogueState.GeneratingResponse);
        await GenerateAndDisplayGenerativeChoices();
        SetDialogueState(DialogueState.WaitingForPlayerInput);
    }

    public void DeclineDialogueRequest()
    {
        DialogueRequestUI.Instance.HideRequest();
    }

    public async void TriggerAgentDialogue(UniversalCharacterController initiator, 
        UniversalCharacterController target)
    {
        if (initiator == null || target == null)
        {
            Debug.LogWarning("DialogueManager: Invalid characters for agent dialogue.");
            return;
        }

        // Get conversation history between these agents
        var conversationHistory = initiator.aiManager.npcData.GetMentalModel().MemoryStream
            .GetConversationHistory(target.characterName);
        
        float relationship = initiator.aiManager.npcData.GetRelationship(target.characterName);
        
        string initiatorMemory = string.Join("\n", 
            conversationHistory.Select(m => m.Content));
        string initiatorReflection = initiator.aiManager.npcData.GetMentalModel().Reflect();

        // Generate initial dialogue
        string initialDialogue = await OpenAIService.Instance.GetAgentResponse(
            initiator.characterName,
            $"Continue conversation with {target.characterName}",
            initiator.aiSettings,
            initiatorMemory,
            initiatorReflection
        );

        // Record the interaction
        float interactionImportance = 0.5f + (relationship * 0.3f);
        initiator.aiManager.npcData.GetMentalModel().AddConversationMemory(
            initiator.characterName,
            initialDialogue,
            target.characterName,
            interactionImportance
        );

        AddToChatLog(initiator.characterName,
            $"<color=#{ColorUtility.ToHtmlStringRGB(initiator.characterColor)}>{initiator.characterName}</color> " +
            $"says to {target.characterName}: \"{initialDialogue}\"");

        // Get target's conversation history and generate response
        var targetConversationHistory = target.aiManager.npcData.GetMentalModel().MemoryStream
            .GetConversationHistory(initiator.characterName);
        
        float targetRelationship = target.aiManager.npcData.GetRelationship(initiator.characterName);
        
        string targetMemory = string.Join("\n", 
            targetConversationHistory.Select(m => m.Content));
        string targetReflection = target.aiManager.npcData.GetMentalModel().Reflect();

        // Generate response
        string response = await OpenAIService.Instance.GetAgentResponse(
            target.characterName,
            initialDialogue,
            target.aiSettings,
            targetMemory,
            targetReflection
        );

        // Record the interaction for target
        float targetInteractionImportance = 0.5f + (targetRelationship * 0.3f);
        target.aiManager.npcData.GetMentalModel().AddConversationMemory(
            target.characterName,
            response,
            initiator.characterName,
            targetInteractionImportance
        );

        AddToChatLog(target.characterName,
            $"<color=#{ColorUtility.ToHtmlStringRGB(target.characterColor)}>{target.characterName}</color> " +
            $"responds: \"{response}\"");
    }

    private class ChatLogEntry
    {
        public string Timestamp;
        public string Speaker;
        public string Message;
    }
}

public class GenerativeChoiceOption
{
    public string Text { get; set; }
    public GenerativeChoiceCategory Category { get; set; }

    public GenerativeChoiceOption(string text, GenerativeChoiceCategory category)
    {
        Text = text;
        Category = category;
    }
}

public enum GenerativeChoiceCategory
{
    Ethical,
    Strategic,
    Emotional,
    Practical,
    Creative,
    Diplomatic,
    RiskTaking
}