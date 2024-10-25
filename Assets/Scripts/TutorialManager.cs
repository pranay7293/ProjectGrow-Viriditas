using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using Photon.Pun;
using System.Linq;
using System.Text.RegularExpressions;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image darkOverlay;
    [SerializeField] private GameObject[] stepContainers;
    [SerializeField] private TextMeshProUGUI stepText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private float fadeSpeed = 0.3f;

    [Header("Chat Demo Elements")]
    [SerializeField] private GameObject characterIcon;
    [SerializeField] private GameObject chatIcon;

    [Header("Action Demo Elements")]
    [SerializeField] private Button[] actionButtons;
    [SerializeField] private Color actionHoverColor = new Color(0.05f, 0.525f, 0.972f);
    [SerializeField] private Color actionDefaultColor = new Color(0.094f, 0.094f, 0.094f);

    private static readonly Color OVERLAY_COLOR = new Color(24f/255f, 24f/255f, 24f/255f, 254f/255f);
    private const string MOONSHOT_STEP_NAME = "StepIcon #2 - Moonshot";
    private const string TUTORIAL_RESOURCES_PATH = "Tutorial/";

    private int currentStepIndex = -1;
    private UniversalCharacterController playerCharacter;
    private ChallengeData currentChallenge;
    private HubData currentHub;
    private bool isChatVisible = false;
    private Image challengeIconImage;

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
        InitializeTutorial();
    }

    private void InitializeTutorial()
    {
        ValidateComponents();
        SetupInitialState();
        SetupActionButtons();
        SetupNavigationButtons();
        FindChallengeIconImage();
    }

    private string FormatResourceName(string challengeTitle)
    {
        // Remove spaces and hyphens
        string formatted = Regex.Replace(challengeTitle, @"[\s-]", "");
        // Debug.Log($"Formatted resource name: {formatted} from title: {challengeTitle}");
        return formatted;
    }

    private Sprite LoadChallengeSprite(string challengeTitle)
    {
        string resourceName = FormatResourceName(challengeTitle);
        Sprite sprite = Resources.Load<Sprite>(TUTORIAL_RESOURCES_PATH + resourceName);
        
        if (sprite == null)
        {
            Debug.LogError($"Failed to load sprite from path: {TUTORIAL_RESOURCES_PATH + resourceName}");
        }
        else
        {
            // Debug.Log($"Successfully loaded sprite for challenge: {challengeTitle}");
        }
        
        return sprite;
    }

    private void FindChallengeIconImage()
    {
        Transform moonshotStep = stepContainers.Select(c => c.transform)
            .FirstOrDefault(t => t.name == MOONSHOT_STEP_NAME);

        if (moonshotStep != null)
        {
            challengeIconImage = moonshotStep.GetComponent<Image>();
            if (challengeIconImage != null)
            {
                // Debug.Log("Found challenge icon image in moonshot step");
            }
            else
            {
                // Debug.LogError($"No Image component found on {MOONSHOT_STEP_NAME}");
            }
        }
        else
        {
            // Debug.LogError($"Could not find step container named {MOONSHOT_STEP_NAME}");
        }
    }

    private void ValidateComponents()
    {
        if (tutorialPanel == null) Debug.LogError("Tutorial Panel not assigned");
        if (darkOverlay == null) Debug.LogError("Dark Overlay not assigned");
        if (stepText == null) Debug.LogError("Step Text not assigned");
        if (stepContainers == null || stepContainers.Length == 0) Debug.LogError("Step Containers not assigned");
    }

    private void SetupInitialState()
    {
        tutorialPanel.SetActive(false);
        darkOverlay.gameObject.SetActive(false);
        darkOverlay.color = OVERLAY_COLOR;

        foreach (var container in stepContainers)
        {
            if (container != null)
                container.SetActive(false);
        }
    }

    private void SetupActionButtons()
    {
        if (actionButtons == null) return;

        foreach (var button in actionButtons)
        {
            if (button == null) continue;

            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = actionDefaultColor;
                SetupButtonEventTrigger(button, buttonImage);
            }
        }
    }

    private void SetupButtonEventTrigger(Button button, Image buttonImage)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>() 
            ?? button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => { OnActionButtonHover(buttonImage, true); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => { OnActionButtonHover(buttonImage, false); });
        trigger.triggers.Add(exitEntry);
    }

    private void SetupNavigationButtons()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextStep);
        if (previousButton != null) previousButton.onClick.AddListener(PreviousStep);
        if (skipButton != null) skipButton.onClick.AddListener(SkipTutorial);
    }

    private UniversalCharacterController GetPlayerCharacter()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedCharacter", out object selectedCharacter))
        {
            Debug.LogError("No character selected in player properties");
            return null;
        }

        string characterName = (string)selectedCharacter;
        // Debug.Log($"Local player selected character: {characterName}");

        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        var playerChar = characters.FirstOrDefault(c => c.characterName == characterName);
        
        if (playerChar == null)
        {
            Debug.LogError($"Could not find character controller for: {characterName}");
        }
        
        return playerChar;
    }

    public void StartTutorial(ChallengeData challenge, HubData hub)
    {
        if (challenge == null || hub == null)
        {
            Debug.LogError("Cannot start tutorial: Challenge or Hub data is null");
            return;
        }

        currentChallenge = challenge;
        currentHub = hub;
        
        playerCharacter = GetPlayerCharacter();
        if (playerCharacter == null) return;

        UpdateChallengeIcon();
        ShowTutorialUI();
        ShowStep(0);
    }

    private void UpdateChallengeIcon()
    {
        if (challengeIconImage == null)
        {
            Debug.LogError("Challenge icon image component not found");
            return;
        }

        // Clear existing sprite
        challengeIconImage.sprite = null;

        // Load sprite from Resources/Tutorial
        Sprite challengeSprite = LoadChallengeSprite(currentChallenge.title);
        if (challengeSprite != null)
        {
            challengeIconImage.sprite = challengeSprite;
            challengeIconImage.preserveAspect = true;
            // Debug.Log($"Set challenge icon for: {currentChallenge.title}");
        }
    }

    private void ShowTutorialUI()
    {
        tutorialPanel.SetActive(true);
        darkOverlay.gameObject.SetActive(true);
        darkOverlay.color = OVERLAY_COLOR;
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= stepContainers.Length) return;

        if (currentStepIndex >= 0 && currentStepIndex < stepContainers.Length)
        {
            stepContainers[currentStepIndex].SetActive(false);
        }

        currentStepIndex = index;
        stepContainers[currentStepIndex].SetActive(true);
        stepText.text = GetStepText(currentStepIndex);

        if (currentStepIndex == 2)
        {
            characterIcon.SetActive(true);
            chatIcon.SetActive(false);
            isChatVisible = false;
        }

        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        previousButton.interactable = (currentStepIndex > 0);
        nextButton.interactable = (currentStepIndex < stepContainers.Length - 1);
    }

    private void OnActionButtonHover(Image buttonImage, bool isHover)
    {
        if (currentStepIndex == 3 && buttonImage != null)
        {
            buttonImage.DOColor(isHover ? actionHoverColor : actionDefaultColor, 0.3f);
        }
    }

    private string GetStepText(int stepIndex)
{
    string coloredCharacterName = $"<color=#{ColorUtility.ToHtmlStringRGB(playerCharacter.GetCharacterColor())}>{playerCharacter.characterName}</color>";
    string coloredRole = $"<color=#{ColorUtility.ToHtmlStringRGB(playerCharacter.GetCharacterColor())}>{playerCharacter.aiSettings.characterRole}</color>";
    string coloredMoonshot = $"<color=#{ColorUtility.ToHtmlStringRGB(currentHub.hubColor)}>{currentChallenge.title}</color>";

    switch (stepIndex)
    {
        case 0:
            return $"You are {coloredCharacterName}. Your role in this simulation is a {coloredRole}.";
        case 1:
            return $"Your moonshot is {coloredMoonshot}.\nPress F2 in-game to view milestones.";
        case 2:
            return "Press E to chat with other characters.\nTry it now!";
        case 3:
            return "Stand still in a location for 5 seconds to reveal available actions.\nActions are how you make progress.";
        case 4:
            return "Press F5 to open EurekaLog and see what happens when AI agents collaborate.";
        default:
            return "";
    }
}

    private void NextStep() => ShowStep(currentStepIndex + 1);
    private void PreviousStep() => ShowStep(currentStepIndex - 1);

    private void SkipTutorial()
{
    var sequence = DOTween.Sequence();
    sequence.Join(darkOverlay.DOFade(0, fadeSpeed));
    sequence.Join(stepText.DOFade(0, fadeSpeed));
    
    if (currentStepIndex >= 0 && currentStepIndex < stepContainers.Length)
    {
        var images = stepContainers[currentStepIndex].GetComponentsInChildren<Image>();
        var texts = stepContainers[currentStepIndex].GetComponentsInChildren<TextMeshProUGUI>();
        
        foreach (var image in images)
        {
            sequence.Join(image.DOFade(0, fadeSpeed));
        }
        
        foreach (var text in texts)
        {
            sequence.Join(text.DOFade(0, fadeSpeed));
        }
    }
    
    sequence.OnComplete(() => {
        tutorialPanel.SetActive(false);
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetUIActive(false);
        }
    });
}

    private void Update()
{
    // Skip with X or Esc
    if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
    {
        SkipTutorial();
    }

    // Handle E key for chat demo
    if (currentStepIndex == 2 && Input.GetKeyDown(KeyCode.E))
    {
        // Toggle chat icon
        isChatVisible = !isChatVisible;
        chatIcon.SetActive(isChatVisible);
    }
}

private void OnEnable()
{
    // Register with InputManager
    if (InputManager.Instance != null)
    {
        InputManager.Instance.SetUIActive(true);
    }
}

private void OnDisable()
{
    // Unregister with InputManager
    if (InputManager.Instance != null)
    {
        InputManager.Instance.SetUIActive(false);
    }
}
}