using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class CharacterProgressBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider[] personalGoalSliders;
    [SerializeField] private Image keyStateOverlay;
    [SerializeField] private TextMeshProUGUI keyStateText;
    [SerializeField] private GameObject cooldownBarObject;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private GameObject locationAcclimationObject;
    [SerializeField] private Image locationAcclimationFill;

    [Header("Settings")]
    [SerializeField] private float collabCooldown = 10f;
    [SerializeField] private Color unfilledColor = new Color(0x4A / 255f, 0x4A / 255f, 0x4A / 255f, 1f);
    [SerializeField] private Color acclimationColor = new Color(0x18 / 255f, 0x18 / 255f, 0x18 / 255f, 1f);
    [SerializeField] private int personalGoalMaxScore = 100;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.25f, 0);

    private Camera mainCamera;
    private UniversalCharacterController characterController;
    private CharacterState currentKeyState = CharacterState.None;
    private float[] personalGoalProgress;

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        characterNameText.text = controller.characterName;
        mainCamera = Camera.main;

        SetupUIElements(controller.characterColor);
        ResetUIState();

        personalGoalProgress = new float[personalGoalSliders.Length];
    }

    private void SetupUIElements(Color characterColor)
    {
        SetupSliders(characterColor);
        SetupKeyStateUI(characterColor);
        SetupLocationAcclimationUI(characterColor);
        SetupCooldownUI();
    }

    private void SetupSliders(Color characterColor)
    {
        foreach (Slider slider in personalGoalSliders)
        {
            SetSliderColors(slider, characterColor);
            slider.maxValue = 1f;
            slider.value = 0f;
        }
    }

    private void SetSliderColors(Slider slider, Color fillColor)
    {
        Image backgroundImage = slider.transform.Find("Background")?.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = unfilledColor;
        }

        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = new Color(fillColor.r, fillColor.g, fillColor.b, 1f);
        }
    }

    private void SetupKeyStateUI(Color characterColor)
    {
        keyStateOverlay.color = new Color(characterColor.r, characterColor.g, characterColor.b, 1f);
        keyStateText.color = Color.white;
    }

    private void SetupLocationAcclimationUI(Color characterColor)
    {
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.type = Image.Type.Filled;
            locationAcclimationFill.fillMethod = Image.FillMethod.Radial360;
            locationAcclimationFill.fillOrigin = (int)Image.Origin360.Bottom;
            locationAcclimationFill.fillAmount = 0f;
            locationAcclimationFill.color = characterColor;
        }
    }

    private void SetupCooldownUI()
    {
        if (cooldownSlider != null)
        {
            cooldownSlider.direction = Slider.Direction.RightToLeft;
            cooldownSlider.maxValue = collabCooldown;
            cooldownSlider.value = collabCooldown;
            Image fillImage = cooldownSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.white;
            }
        }
    }

    private void ResetUIState()
    {
        UpdateKeyState(CharacterState.None);
        if (cooldownBarObject != null) cooldownBarObject.SetActive(false);
        if (locationAcclimationObject != null) locationAcclimationObject.SetActive(false);
    }

    private void LateUpdate()
    {
        UpdateProgressBarPosition();
        UpdateProgressBarOrientation();
        UpdatePersonalGoals();
    }

    private void UpdateProgressBarPosition()
    {
        if (characterController != null)
        {
            transform.position = characterController.transform.position + offset;
        }
    }

    private void UpdateProgressBarOrientation()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void UpdatePersonalGoals()
    {
        for (int i = 0; i < personalGoalSliders.Length; i++)
        {
            if (personalGoalSliders[i] != null)
            {
                StartCoroutine(SmoothSliderUpdate(personalGoalSliders[i], personalGoalProgress[i]));
            }
        }
    }

    private IEnumerator SmoothSliderUpdate(Slider slider, float targetValue)
    {
        float elapsedTime = 0;
        float startValue = slider.value;
        while (elapsedTime < 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / 0.5f;
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }
        slider.value = targetValue;
    }

    public void SetPersonalGoalProgress(float[] progress)
    {
        if (progress.Length != personalGoalSliders.Length)
        {
            Debug.LogError("Mismatch in personal goal progress array length");
            return;
        }
        for (int i = 0; i < progress.Length; i++)
        {
            StartCoroutine(SmoothSliderUpdate(personalGoalSliders[i], progress[i]));
        }
    }

    public void UpdateKeyState(CharacterState state)
    {
        CharacterState highestPriorityState = GetHighestPriorityState(state);
        
        if (highestPriorityState == currentKeyState) return;

        currentKeyState = highestPriorityState;
        bool showKeyState = (int)highestPriorityState >= 4;
        keyStateOverlay.gameObject.SetActive(showKeyState);
        
        string displayText = GetDisplayTextForState(highestPriorityState);
        keyStateText.text = showKeyState ? displayText : "";

        HandleSpecialStates(highestPriorityState);
    }

    private CharacterState GetHighestPriorityState(CharacterState state)
    {
        for (int i = Enum.GetValues(typeof(CharacterState)).Length - 1; i >= 0; i--)
        {
            CharacterState currentState = (CharacterState)(1 << i);
            if (state.HasFlag(currentState))
            {
                return currentState;
            }
        }
        return CharacterState.None;
    }

    private string GetDisplayTextForState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.PerformingAction:
                return "Performing Action";
            case CharacterState.Collaborating:
                return "Collabing";
            case CharacterState.FormingGroup:
                return "Forming Group";
            case CharacterState.Cooldown:
                return "Cooling Down";
            default:
                return state.ToString();
        }
    }

    private void HandleSpecialStates(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Cooldown:
                SetCooldown(collabCooldown);
                break;
            case CharacterState.Acclimating:
                StartAcclimation();
                break;
            case CharacterState.None:
                ResetUIState();
                break;
        }
    }

    public void SetCooldown(float duration)
    {
        if (cooldownBarObject != null)
        {
            cooldownBarObject.SetActive(true);
        }
        if (cooldownSlider != null)
        {
            cooldownSlider.maxValue = duration;
            cooldownSlider.value = duration;
            StartCoroutine(UpdateCooldown());
        }
    }

    private IEnumerator UpdateCooldown()
    {
        while (cooldownSlider != null && cooldownSlider.value > 0)
        {
            cooldownSlider.value -= Time.deltaTime;
            yield return null;
        }
        if (cooldownBarObject != null)
        {
            cooldownBarObject.SetActive(false);
        }
        UpdateKeyState(CharacterState.None);
    }

    public void StartAcclimation()
    {
        if (locationAcclimationObject != null)
        {
            locationAcclimationObject.SetActive(true);
        }
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.fillAmount = 1f;
        }
    }

    public void UpdateAcclimationProgress(float progress)
    {
        if (locationAcclimationFill != null && locationAcclimationObject.activeSelf)
        {
            locationAcclimationFill.fillAmount = progress;
        }
    }

    public void EndAcclimation()
    {
        if (locationAcclimationObject != null)
        {
            locationAcclimationObject.SetActive(false);
        }
        UpdateKeyState(CharacterState.None);
    }
}