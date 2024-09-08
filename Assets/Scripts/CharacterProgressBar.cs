using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CharacterProgressBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider[] personalGoalSliders;
    [SerializeField] private Image keyStateOverlay;
    [SerializeField] private TextMeshProUGUI keyStateText;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image locationAcclimationFill;

    [Header("Settings")]
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private Color unfilledColor = new Color(0x4A / 255f, 0x4A / 255f, 0x4A / 255f, 1f);
    [SerializeField] private Color acclimationColor = new Color(0x18 / 255f, 0x18 / 255f, 0x18 / 255f, 1f);
    [SerializeField] private int personalGoalMaxScore = 100;

    private Camera mainCamera;
    private UniversalCharacterController characterController;
    private UniversalCharacterController.CharacterState currentKeyState = UniversalCharacterController.CharacterState.None;

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        characterNameText.text = controller.characterName;
        mainCamera = Camera.main;

        SetupUIElements(controller.characterColor);
        ResetUIState();
    }

    private void SetupUIElements(Color characterColor)
    {
        SetupSliders(characterColor);
        SetupKeyStateUI(characterColor);
        SetupLocationAcclimationUI(characterColor);
    }

    private void SetupSliders(Color characterColor)
    {
        foreach (Slider slider in personalGoalSliders)
        {
            SetSliderColors(slider, characterColor);
            slider.maxValue = 1f;
            slider.value = 0f;
        }

        SetSliderColors(cooldownSlider, Color.white);
        cooldownSlider.maxValue = collabCooldown;
        cooldownSlider.value = 0f;
    }

    public void SetCooldown(float duration)
    {
        cooldownSlider.gameObject.SetActive(true);
        cooldownSlider.maxValue = duration;
        cooldownSlider.value = duration;
        StartCoroutine(UpdateCooldown());
    }

    private IEnumerator UpdateCooldown()
    {
        while (cooldownSlider.value > 0)
        {
            cooldownSlider.value -= Time.deltaTime;
            yield return null;
        }
        cooldownSlider.gameObject.SetActive(false);
        UpdateKeyState(UniversalCharacterController.CharacterState.None);
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

    private void ResetUIState()
    {
        UpdateKeyState(UniversalCharacterController.CharacterState.None);
        cooldownSlider.gameObject.SetActive(false);
        if (locationAcclimationFill != null) locationAcclimationFill.gameObject.SetActive(false);
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

    private void LateUpdate()
    {
        UpdateProgressBarOrientation();
        UpdatePersonalGoals();
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
            if (i < characterController.PersonalProgress.Length)
            {
                float normalizedProgress = characterController.PersonalProgress[i] / personalGoalMaxScore;
                personalGoalSliders[i].value = normalizedProgress;
            }
        }
    }

    public void UpdateKeyState(UniversalCharacterController.CharacterState state)
    {
        if (state == currentKeyState) return;

        currentKeyState = state;
        bool showKeyState = (int)state >= 4;
        keyStateOverlay.gameObject.SetActive(showKeyState);
        keyStateText.text = showKeyState ? state.ToString() : "";

        switch (state)
        {
            case UniversalCharacterController.CharacterState.Cooldown:
                SetCooldown(collabCooldown);
                break;
            case UniversalCharacterController.CharacterState.Acclimating:
                StartAcclimation();
                break;
            case UniversalCharacterController.CharacterState.None:
                ResetUIState();
                break;
        }
    }

    public void StartAcclimation()
    {
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.gameObject.SetActive(true);
            locationAcclimationFill.fillAmount = 1f;
        }
    }

    public void UpdateAcclimationProgress(float progress)
    {
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.fillAmount = progress;
        }
    }

    public void EndAcclimation()
    {
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.gameObject.SetActive(false);
        }
        UpdateKeyState(UniversalCharacterController.CharacterState.None);
    }
}