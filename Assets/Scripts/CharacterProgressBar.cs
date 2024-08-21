using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public enum KeyState
{
    None,
    PerformingAction,
    Collaborating,
    Cooldown,
    Chatting,
    Acclimating
}

public class CharacterProgressBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider[] personalGoalSliders;
    [SerializeField] private Image keyStateOverlay;
    [SerializeField] private TextMeshProUGUI keyStateText;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private RectTransform locationAcclimationBackground;
    [SerializeField] private Image locationAcclimationFill;

    [Header("Settings")]
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private Color unfilledColor = new Color(0x4A / 255f, 0x4A / 255f, 0x4A / 255f, 1f); // #4A4A4A with full alpha
    [SerializeField] private Color acclimationBackgroundColor = new Color(0x18 / 255f, 0x18 / 255f, 0x18 / 255f, 1f); // #181818 with full alpha

    private Camera mainCamera;
    private UniversalCharacterController characterController;

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
        SetupLocationAcclimationUI();
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

    private void SetupKeyStateUI(Color characterColor)
    {
        keyStateOverlay.color = new Color(characterColor.r, characterColor.g, characterColor.b, 1f);
        keyStateText.color = Color.white;
    }

    private void SetupLocationAcclimationUI()
    {
        if (locationAcclimationBackground != null)
        {
            Image backgroundImage = locationAcclimationBackground.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = acclimationBackgroundColor;
                backgroundImage.type = Image.Type.Simple;
            }
        }

        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.type = Image.Type.Filled;
            locationAcclimationFill.fillMethod = Image.FillMethod.Radial360;
            locationAcclimationFill.fillOrigin = (int)Image.Origin360.Bottom;
            locationAcclimationFill.fillAmount = 1f;
            locationAcclimationFill.raycastTarget = false;

            RectTransform fillRect = locationAcclimationFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
        }
    }

    private void ResetUIState()
    {
        SetKeyState(KeyState.None);
        cooldownSlider.gameObject.SetActive(false);
        if (locationAcclimationBackground != null) locationAcclimationBackground.gameObject.SetActive(false);
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
                personalGoalSliders[i].value = characterController.PersonalProgress[i];
            }
        }
    }

    public void SetKeyState(KeyState state)
    {
        keyStateOverlay.gameObject.SetActive(state != KeyState.None);
        if (state != KeyState.None)
        {
            keyStateText.text = state.ToString();
        }
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
        SetKeyState(KeyState.None);
    }

    public void StartAcclimation(Color locationColor)
    {
        if (locationAcclimationBackground != null) locationAcclimationBackground.gameObject.SetActive(true);
        if (locationAcclimationFill != null)
        {
            locationAcclimationFill.gameObject.SetActive(true);
            locationAcclimationFill.color = new Color(locationColor.r, locationColor.g, locationColor.b, 1f);
            locationAcclimationFill.fillAmount = 1f;
        }
        SetKeyState(KeyState.Acclimating);
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
        if (locationAcclimationBackground != null) locationAcclimationBackground.gameObject.SetActive(false);
        if (locationAcclimationFill != null) locationAcclimationFill.gameObject.SetActive(false);
        SetKeyState(KeyState.None);
    }
}