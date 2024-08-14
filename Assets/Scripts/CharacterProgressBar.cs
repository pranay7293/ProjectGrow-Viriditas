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

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        characterNameText.text = controller.characterName;
        mainCamera = Camera.main;

        // Set colors based on character color
        Color characterColor = controller.characterColor;
        foreach (Slider slider in personalGoalSliders)
        {
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.wholeNumbers = true;
            slider.fillRect.GetComponent<Image>().color = characterColor;
        }

        // Setup cooldown slider
        cooldownSlider.maxValue = COLLAB_COOLDOWN;
        cooldownSlider.value = 0f;
        cooldownSlider.fillRect.GetComponent<Image>().color = Color.white;

        keyStateOverlay.color = new Color(characterColor.r, characterColor.g, characterColor.b, 0.8f);

        // Hide key state and cooldown initially
        SetKeyState("");
        cooldownSlider.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        UpdateProgressBars();
    }

    private void UpdateProgressBars()
    {
        for (int i = 0; i < personalGoalSliders.Length; i++)
        {
            personalGoalSliders[i].value = characterController.PersonalProgress[i];
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