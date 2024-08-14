using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LocationActionUI : MonoBehaviour
{
    public static LocationActionUI Instance { get; private set; }

    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ActionButton[] actionButtons;

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
        closeButton.onClick.AddListener(HideActions);
        actionPanel.SetActive(false);
        outcomeText.gameObject.SetActive(false);

        for (int i = 0; i < actionButtons.Length; i++)
        {
            int index = i;
            actionButtons[i].Button.onClick.AddListener(() => OnActionButtonClicked(index));
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
                actionButtons[i].gameObject.SetActive(true);
                SetupActionButton(actionButtons[i], availableActions[i]);
            }
            else
            {
                actionButtons[i].gameObject.SetActive(false);
            }
        }

        actionPanel.SetActive(true);
        UpdateCollabUI();
    }

    private void SetupActionButton(ActionButton button, LocationManager.LocationAction action)
    {
        button.ActionName.text = action.actionName;
        button.ActionIcon.sprite = action.actionIcon;
        button.ActionIcon.color = Color.white;
        button.ActionDuration.text = $"{action.duration} SEC";
        button.CircularProgressBar.fillAmount = 0;
        button.CollabButton.gameObject.SetActive(false);
    }

    private void OnActionButtonClicked(int index)
    {
    LocationManager.LocationAction selectedAction = currentLocation.GetAvailableActions(currentCharacter.aiSettings.characterRole)[index];
    currentCharacter.StartAction(selectedAction);
    }

    public void UpdateActionProgress(string actionName, float progress)
    {
    ActionButton button = System.Array.Find(actionButtons, b => b.ActionName.text == actionName);
    if (button != null)
    {
        button.CircularProgressBar.fillAmount = progress;
    }
    }

    public void ShowOutcome(string outcome)
    {
        outcomeText.gameObject.SetActive(true);
        outcomeText.text = outcome;
        Invoke(nameof(HideOutcome), 2f);
    }

    private void HideOutcome()
    {
        outcomeText.gameObject.SetActive(false);
    }

    public void HideActions()
    {
        actionPanel.SetActive(false);
        currentCharacter = null;
        currentLocation = null;
    }

    public void UpdateActionDescription(string description)
    {
        actionDescriptionText.text = description;
    }

    private void UpdateCollabUI()
    {
    List<UniversalCharacterController> eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(currentCharacter);
    
    foreach (var button in actionButtons)
    {
        button.CollabButton.gameObject.SetActive(eligibleCollaborators.Count > 0);
        button.CollabButton.onClick.RemoveAllListeners();
        button.CollabButton.onClick.AddListener(() => InitiateCollab(button.ActionName.text));
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

[System.Serializable]
public class ActionButton
{
    public GameObject gameObject;
    public Image CircularBackground;
    public Image CircularProgressBar;
    public Image ActionIcon;
    public TextMeshProUGUI ActionName;
    public TextMeshProUGUI ActionDuration;
    public Button Button;
    public Button CollabButton;
}