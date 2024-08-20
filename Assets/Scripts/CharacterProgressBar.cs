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
    [SerializeField] private GameObject locationAcclimationTime;
    [SerializeField] private Image locationAcclimationFill;

    [Header("Settings")]
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private Color unfilledColor = new Color(0x4A / 255f, 0x4A / 255f, 0x4A / 255f); // #4A4A4A

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
        foreach (Slider slider in personalGoalSliders)
        {
            SetSliderColors(slider, characterColor);
            slider.maxValue = 1f;
            slider.value = 0f;
        }

        SetSliderColors(cooldownSlider, Color.white);
        cooldownSlider.maxValue = collabCooldown;
        cooldownSlider.value = 0f;

        keyStateOverlay.color = characterColor;
        keyStateText.color = Color.white;
    }

    private void ResetUIState()
    {
        SetKeyState(KeyState.None);
        cooldownSlider.gameObject.SetActive(false);
        locationAcclimationTime.SetActive(false);
    }

    private void SetSliderColors(Slider slider, Color fillColor)
    {
        if (slider.transform.Find("Background")?.GetComponent<Image>() is Image backgroundImage)
        {
            backgroundImage.color = unfilledColor;
        }

        if (slider.fillRect.GetComponent<Image>() is Image fillImage)
        {
            fillImage.color = fillColor;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        UpdatePersonalGoals();
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

    public void UpdateAcclimationProgress(float progress, Color locationColor)
    {
        if (!locationAcclimationTime.activeSelf)
        {
            locationAcclimationTime.SetActive(true);
            SetKeyState(KeyState.Acclimating);
        }

        locationAcclimationFill.fillAmount = progress;
        locationAcclimationFill.color = locationColor;

        if (progress >= 1f)
        {
            locationAcclimationTime.SetActive(false);
            SetKeyState(KeyState.None);
        }
    }
}