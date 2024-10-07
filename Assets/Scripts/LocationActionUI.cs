using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.EventSystems;

public class LocationActionUI : MonoBehaviour
{
    public static LocationActionUI Instance { get; private set; }

    [System.Serializable]
    public class ActionButton
    {
        public Button button;
        public Image circularBackground;
        public Image circularProgressBar;
        public Image actionIcon;
        public TextMeshProUGUI actionNameText;
        public TextMeshProUGUI actionDurationText;
        public Button collabButton;
        public GameObject collabOptionsPanel;
        public Button[] collabOptionButtons;
        public Image[] collabOptionIcons;
    }

    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private ActionButton[] actionButtons;
    [SerializeField] private float outcomeDisplayDuration = 2f;
    [SerializeField] private GameObject guideTextBox;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private float guideDisplayDuration = 2f;
    [SerializeField] private float guideFadeDuration = 0.5f;
    [SerializeField] private float hoverTransitionDuration = 0.3f;
    [SerializeField] private float bounceScale = 1.05f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float debounceDuration = 0.5f;

    private UniversalCharacterController currentCharacter;
    private LocationManager currentLocation;
    private Dictionary<string, List<UniversalCharacterController>> eligibleCollaborators = new Dictionary<string, List<UniversalCharacterController>>();
    private Color defaultButtonColor;
    private Color hoverButtonColor;
    private Coroutine showActionsCoroutine;

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
        if (actionPanel != null)
        {
            actionPanel.SetActive(false);
        }
        outcomeText.gameObject.SetActive(false);
        if (guideTextBox != null)
        {
            guideTextBox.SetActive(false);
        }

        defaultButtonColor = actionButtons[0].circularBackground.color;
        InitializeActionButtons();
    }

    private void InitializeActionButtons()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int index = i;
            ActionButton actionButton = actionButtons[i];

            if (actionButton.button != null)
            {
                actionButton.button.onClick.RemoveAllListeners();
                actionButton.button.onClick.AddListener(() => OnActionButtonClicked(index));

                // Add hover effects
                EventTrigger trigger = actionButton.button.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data, index); });
                trigger.triggers.Add(enterEntry);

                EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data, index); });
                trigger.triggers.Add(exitEntry);
            }

            if (actionButton.collabButton != null)
            {
                actionButton.collabButton.onClick.RemoveAllListeners();
                actionButton.collabButton.onClick.AddListener(() => ToggleCollabOptions(index));
            }

            if (actionButton.collabOptionsPanel != null)
            {
                actionButton.collabOptionsPanel.SetActive(false);
            }

            for (int j = 0; j < actionButton.collabOptionButtons.Length; j++)
            {
                int optionIndex = j;
                if (actionButton.collabOptionButtons[j] != null)
                {
                    actionButton.collabOptionButtons[j].onClick.RemoveAllListeners();
                    actionButton.collabOptionButtons[j].onClick.AddListener(() => OnCollabOptionClicked(index, optionIndex));
                }
            }
        }
    }

    public void ShowActionsForLocation(UniversalCharacterController character, LocationManager location)
    {
        if (showActionsCoroutine != null)
        {
            StopCoroutine(showActionsCoroutine);
        }
        showActionsCoroutine = StartCoroutine(ShowActionsCoroutine(character, location));
    }

    private IEnumerator ShowActionsCoroutine(UniversalCharacterController character, LocationManager location)
    {
        yield return new WaitForSeconds(debounceDuration);

        if (!location.IsCharacterInLocation(character) || character.HasState(UniversalCharacterController.CharacterState.Acclimating))
        {
            yield break;
        }

        currentCharacter = character;
        currentLocation = location;

        if (locationNameText != null)
        {
            locationNameText.text = location.locationName;
        }
        List<LocationManager.LocationAction> availableActions = location.GetAvailableActions(character.aiSettings.characterRole);

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (i < availableActions.Count)
            {
                actionButtons[i].button.gameObject.SetActive(true);
                SetupActionButton(actionButtons[i], availableActions[i]);
            }
            else
            {
                actionButtons[i].button.gameObject.SetActive(false);
            }
        }

        hoverButtonColor = location.locationColor;
        actionPanel.SetActive(true);
        UpdateCollabUI();
        InputManager.Instance.SetUIActive(true);

        // Fade in the action panel
        actionPanel.GetComponent<CanvasGroup>().alpha = 0f;
        actionPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
    }

    private void SetupActionButton(ActionButton button, LocationManager.LocationAction action)
    {
        if (button == null || action == null)
        {
            Debug.LogError("Button or action is null in SetupActionButton");
            return;
        }

        button.actionNameText.text = action.actionName;
        button.actionIcon.sprite = action.actionIcon;
        button.actionIcon.color = Color.white;
        button.actionDurationText.text = $"{action.duration}";
        button.circularProgressBar.fillAmount = 0;
        button.collabButton.gameObject.SetActive(false);
        button.circularBackground.color = defaultButtonColor;
    }

    private void OnActionButtonClicked(int index)
    {
        if (currentLocation == null)
        {
            Debug.LogError("CurrentLocation is null in OnActionButtonClicked");
            return;
        }

        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);
        if (index < 0 || index >= availableActions.Count)
        {
            Debug.LogError($"Invalid action index: {index}");
            return;
        }

        LocationManager.LocationAction selectedAction = availableActions[index];
        currentCharacter.StartAction(selectedAction);
    }

    private void ToggleCollabOptions(int actionIndex)
    {
        ActionButton actionButton = actionButtons[actionIndex];
        if (actionButton.collabOptionsPanel != null)
        {
            bool isActive = !actionButton.collabOptionsPanel.activeSelf;
            actionButton.collabOptionsPanel.SetActive(isActive);

            if (isActive)
            {
                UpdateCollabOptionButtons(actionIndex);
            }
        }
    }

    private void UpdateCollabOptionButtons(int actionIndex)
    {
        ActionButton actionButton = actionButtons[actionIndex];
        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);
        string actionName = availableActions[actionIndex].actionName;

        if (eligibleCollaborators.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            for (int i = 0; i < actionButton.collabOptionButtons.Length; i++)
            {
                if (i < collaborators.Count)
                {
                    if (actionButton.collabOptionButtons[i] != null)
                    {
                        actionButton.collabOptionButtons[i].gameObject.SetActive(true);
                        if (actionButton.collabOptionIcons[i] != null)
                        {
                            actionButton.collabOptionIcons[i].color = collaborators[i].GetCharacterColor();
                            AnimateCollabIcon animator = actionButton.collabOptionIcons[i].GetComponent<AnimateCollabIcon>();
                            if (animator != null)
                            {
                                animator.StartAnimation();
                            }
                        }
                    }
                }
                else
                {
                    if (actionButton.collabOptionButtons[i] != null)
                    {
                        actionButton.collabOptionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnCollabOptionClicked(int actionIndex, int collaboratorIndex)
    {
        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);
        string actionName = availableActions[actionIndex].actionName;

        if (eligibleCollaborators.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            if (collaboratorIndex < collaborators.Count)
            {
                UniversalCharacterController collaborator = collaborators[collaboratorIndex];
                currentCharacter.InitiateCollab(actionName, collaborator);
                ToggleCollabOptions(actionIndex);
            }
        }
    }

    public void UpdateActionProgress(string actionName, float progress)
    {
        ActionButton button = System.Array.Find(actionButtons, b => b.actionNameText.text == actionName);
        if (button != null && button.circularProgressBar != null)
        {
            button.circularProgressBar.fillAmount = progress;
        }
    }

    public void ShowOutcome(string outcome)
    {
        outcomeText.gameObject.SetActive(true);
        outcomeText.text = outcome;
        StartCoroutine(HideOutcomeAfterDelay());
    }

    private IEnumerator HideOutcomeAfterDelay()
    {
        yield return new WaitForSeconds(outcomeDisplayDuration);
        outcomeText.gameObject.SetActive(false);
        HideActions();
    }

    public void HideActions()
    {
        if (showActionsCoroutine != null)
        {
            StopCoroutine(showActionsCoroutine);
        }

        // Fade out the action panel
        actionPanel.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() =>
        {
            actionPanel.SetActive(false);
            currentCharacter = null;
            currentLocation = null;
            InputManager.Instance.SetUIActive(false);
        });
    }

    private void UpdateCollabUI()
    {
        eligibleCollaborators.Clear();
        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);

        bool showGuide = false;

        for (int i = 0; i < availableActions.Count; i++)
        {
            string actionName = availableActions[i].actionName;
            List<UniversalCharacterController> collaborators = CollabManager.Instance.GetEligibleCollaborators(currentCharacter);
            
            if (collaborators.Count > 0)
            {
                eligibleCollaborators[actionName] = collaborators;
                actionButtons[i].collabButton.gameObject.SetActive(true);
                showGuide = true;
            }
            else
            {
                actionButtons[i].collabButton.gameObject.SetActive(false);
            }
        }

        if (showGuide)
        {
            ShowGuide();
        }
    }

    private void ShowGuide()
    {
        if (guideTextBox != null && guideText != null)
        {
            guideTextBox.SetActive(true);
            StartCoroutine(FadeGuide(true));
        }
    }

    private IEnumerator FadeGuide(bool fadeIn)
    {
        float elapsedTime = 0f;
        Color startColor = guideText.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, fadeIn ? 1f : 0f);

        while (elapsedTime < guideFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, targetColor.a, elapsedTime / guideFadeDuration);
            guideText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        guideText.color = targetColor;

        if (fadeIn)
        {
            yield return new WaitForSeconds(guideDisplayDuration);
            StartCoroutine(FadeGuide(false));
        }
        else
        {
            guideTextBox.SetActive(false);
        }
    }

    private void OnPointerEnter(PointerEventData eventData, int index)
    {
        ActionButton button = actionButtons[index];
        button.circularBackground.DOColor(hoverButtonColor, hoverTransitionDuration);
        button.button.transform.DOScale(bounceScale, bounceDuration);

        // Show action description
        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);
        if (index < availableActions.Count)
        {
            actionDescriptionText.text = availableActions[index].description;
            actionDescriptionText.DOFade(1f, hoverTransitionDuration);
        }
    }

    private void OnPointerExit(PointerEventData eventData, int index)
    {
        ActionButton button = actionButtons[index];
        button.circularBackground.DOColor(defaultButtonColor, hoverTransitionDuration);
        button.button.transform.DOScale(1f, bounceDuration);

        // Hide action description
        actionDescriptionText.DOFade(0f, hoverTransitionDuration);
    }
}