using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterProgressBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider[] personalGoalSliders;
    [SerializeField] private Image keyStateOverlay;
    [SerializeField] private TextMeshProUGUI keyStateText;
    [SerializeField] private Slider cooldownSlider;

    private Camera mainCamera;
    private UniversalCharacterController characterController;

    private const float COLLAB_COOLDOWN = 45f;
    private Color unfilledColor = new Color(0x4A / 255f, 0x4A / 255f, 0x4A / 255f); // #4A4A4A

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        characterNameText.text = controller.characterName;
        mainCamera = Camera.main;

        // Set colors based on character color
        Color characterColor = controller.characterColor;
        foreach (Slider slider in personalGoalSliders)
        {
            SetSliderColors(slider, characterColor);
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.wholeNumbers = true;
        }

        // Setup cooldown slider
        SetSliderColors(cooldownSlider, Color.white);
        cooldownSlider.maxValue = COLLAB_COOLDOWN;
        cooldownSlider.value = 0f;

        // Setup key state overlay
        keyStateOverlay.color = characterColor;
        keyStateText.color = Color.white;

        // Hide key state and cooldown initially
        SetKeyState("");
        cooldownSlider.gameObject.SetActive(false);
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

    public void SetKeyState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            keyStateOverlay.gameObject.SetActive(false);
        }
        else
        {
            keyStateOverlay.gameObject.SetActive(true);
            keyStateText.text = state;
        }
    }

    public void SetCooldown(float duration)
    {
        cooldownSlider.gameObject.SetActive(true);
        cooldownSlider.value = duration;
        StartCoroutine(UpdateCooldown());
    }

    private System.Collections.IEnumerator UpdateCooldown()
    {
        while (cooldownSlider.value > 0)
        {
            cooldownSlider.value -= Time.deltaTime;
            yield return null;
        }
        cooldownSlider.gameObject.SetActive(false);
    }
}