using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;

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
    }

    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private ActionButton[] actionButtons;
    [SerializeField] private float outcomeDisplayDuration = 2f;

    private UniversalCharacterController currentCharacter;
    private LocationManager currentLocation;

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

                EventTrigger trigger = actionButton.button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = actionButton.button.gameObject.AddComponent<EventTrigger>();
                }

                EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((data) => { OnActionButtonHover(index); });
                trigger.triggers.Add(entryEnter);

                EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((data) => { ClearActionDescription(); });
                trigger.triggers.Add(entryExit);
            }

            if (actionButton.collabButton != null)
            {
                actionButton.collabButton.onClick.RemoveAllListeners();
                actionButton.collabButton.onClick.AddListener(() => InitiateCollab(actionButton.actionNameText.text));
            }
        }
    }

    public void ShowActionsForLocation(UniversalCharacterController character, LocationManager location)
    {
        currentCharacter = character;
        currentLocation = location;

        locationNameText.text = location.locationName;
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

        actionPanel.SetActive(true);
        UpdateCollabUI();
        InputManager.Instance.SetUIActive(true);
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

    private void OnActionButtonHover(int index)
    {
        if (currentLocation == null) return;

        List<LocationManager.LocationAction> availableActions = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole);
        if (index < 0 || index >= availableActions.Count) return;

        LocationManager.LocationAction action = availableActions[index];
        actionDescriptionText.text = action.description;
    }

    private void ClearActionDescription()
    {
        actionDescriptionText.text = "";
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
        StartCoroutine(HideOutcomeAndCloseUI());
    }

    private IEnumerator HideOutcomeAndCloseUI()
    {
        yield return new WaitForSeconds(outcomeDisplayDuration);
        outcomeText.gameObject.SetActive(false);
        HideActions();
    }

    public void HideActions()
    {
        actionPanel.SetActive(false);
        currentCharacter = null;
        currentLocation = null;
        InputManager.Instance.SetUIActive(false);
    }

    private void UpdateCollabUI()
    {
        List<UniversalCharacterController> eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(currentCharacter);
        
        foreach (var button in actionButtons)
        {
            if (button.collabButton != null)
            {
                button.collabButton.gameObject.SetActive(eligibleCollaborators.Count > 0);
            }
        }
    }

    private void InitiateCollab(string actionName)
    {
        currentCharacter.InitiateCollab(actionName);
        ShowCollabPromptForNearbyCharacters(actionName);
    }

    private void ShowCollabPromptForNearbyCharacters(string actionName)
    {
        List<UniversalCharacterController> eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(currentCharacter);
        
        foreach (var collaborator in eligibleCollaborators)
        {
            if (collaborator.IsPlayerControlled)
            {
                CollabPromptUI.Instance.ShowPrompt(collaborator, actionName);
            }
            else
            {
                AIManager aiManager = collaborator.GetComponent<AIManager>();
                if (aiManager != null && aiManager.DecideOnCollaboration(actionName))
                {
                    collaborator.JoinCollab(actionName);
                }
            }
        }
    }
}