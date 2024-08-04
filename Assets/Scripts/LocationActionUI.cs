using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LocationActionUI : MonoBehaviour
{
    public static LocationActionUI Instance { get; private set; }

    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Button actionButtonPrefab;
    [SerializeField] private Transform actionButtonContainer;
    [SerializeField] private Button closeButton;

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
            
            // Set up the OnClick event
            newButton.onClick.AddListener(() => OnActionButtonClicked(action));

            // Set up success probability and time indicator
            TextMeshProUGUI probabilityText = newButton.transform.Find("ProbabilityText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timeText = newButton.transform.Find("TimeText").GetComponent<TextMeshProUGUI>();
            
            float successRate = CalculateSuccessRate(currentCharacter, action);
            probabilityText.text = $"{successRate:P0} Success";
            timeText.text = $"{action.duration}s";
        }
    }

    private void OnActionButtonClicked(LocationManager.LocationAction action)
    {
        Debug.Log($"Action clicked: {action.actionName}");
        currentCharacter.PerformAction(action.actionName);
        RiskRewardManager.Instance.EvaluateActionOutcome(currentCharacter, action);
        HideActions();
    }

    private float CalculateSuccessRate(UniversalCharacterController character, LocationManager.LocationAction action)
    {
        float baseRate = action.baseSuccessRate;
        float roleBonus = (character.aiSettings.characterRole == action.requiredRole) ? 0.2f : 0f;
        return Mathf.Clamp01(baseRate + roleBonus);
    }
}