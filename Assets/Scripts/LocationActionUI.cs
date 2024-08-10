using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class LocationActionUI : MonoBehaviour
{
    public static LocationActionUI Instance { get; private set; }

    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Button actionButtonPrefab;
    [SerializeField] private Transform actionButtonContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image actionProgressBar;
    [SerializeField] private TextMeshProUGUI actionProgressText;
    [SerializeField] private TextMeshProUGUI outcomeText;

    private UniversalCharacterController currentCharacter;
    private List<LocationManager.LocationAction> currentActions;

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
        actionProgressBar.gameObject.SetActive(false);
        actionProgressText.gameObject.SetActive(false);
        outcomeText.gameObject.SetActive(false);
    }

    public void ShowActionsForLocation(UniversalCharacterController character, LocationManager location)
    {
        currentCharacter = character;
        currentActions = location.GetAvailableActions(character.aiSettings.characterRole);

        locationNameText.text = location.locationName;
        ClearActionButtons();
        CreateActionButtons();

        actionPanel.SetActive(true);
    }

    public void HideActions()
    {
        actionPanel.SetActive(false);
        currentCharacter = null;
        currentActions = null;
    }

    private void ClearActionButtons()
    {
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateActionButtons()
    {
        foreach (var action in currentActions)
        {
            Button newButton = Instantiate(actionButtonPrefab, actionButtonContainer);
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = action.actionName;
            
            Image iconImage = newButton.transform.Find("ActionIcon").GetComponent<Image>();
            iconImage.sprite = action.actionIcon;

            TextMeshProUGUI durationText = newButton.transform.Find("DurationText").GetComponent<TextMeshProUGUI>();
            durationText.text = $"{action.duration} SEC";

            TextMeshProUGUI successRateText = newButton.transform.Find("SuccessRateText").GetComponent<TextMeshProUGUI>();
            successRateText.text = $"{action.baseSuccessRate * 100}%";

            newButton.onClick.AddListener(() => OnActionButtonClicked(action));
        }
    }

    private void OnActionButtonClicked(LocationManager.LocationAction action)
    {
        currentCharacter.photonView.RPC("StartAction", RpcTarget.All, currentActions.IndexOf(action), currentCharacter.photonView.ViewID);
        ShowActionProgress();
    }

    private void ShowActionProgress()
    {
        actionProgressBar.gameObject.SetActive(true);
        actionProgressText.gameObject.SetActive(true);
        ClearActionButtons();
    }

    public void UpdateActionProgress(float progress)
    {
        actionProgressBar.fillAmount = progress;
        actionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    public void ShowOutcome(string outcome)
    {
        actionProgressBar.gameObject.SetActive(false);
        actionProgressText.gameObject.SetActive(false);
        outcomeText.gameObject.SetActive(true);
        outcomeText.text = outcome;

        Invoke(nameof(HideOutcome), 2f);
    }

    public void HideOutcome()
    {
        outcomeText.gameObject.SetActive(false);
        ShowActionsForLocation(currentCharacter, currentCharacter.currentLocation);
    }
}