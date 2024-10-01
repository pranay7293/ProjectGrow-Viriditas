using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EurekaUI : MonoBehaviour
{
    public static EurekaUI Instance { get; private set; }

    [SerializeField] private GameObject eurekaNotificationPanel;
    [SerializeField] private TextMeshProUGUI eurekaTitleText;
    [SerializeField] private TextMeshProUGUI eurekaDescriptionText;
    [SerializeField] private GameObject eurekaLogPromptObject;
    [SerializeField] private TextMeshProUGUI eurekaLogPromptText;
    [SerializeField] private float notificationDuration = 5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure the notification is hidden at the start
        HideNotification();
    }

    private void HideNotification()
    {
        if (eurekaNotificationPanel != null)
            eurekaNotificationPanel.SetActive(false);
        if (eurekaLogPromptObject != null)
            eurekaLogPromptObject.SetActive(false);
    }

    public void DisplayEurekaNotification(List<UniversalCharacterController> collaborators, string actionName)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("EurekaUI GameObject is inactive. Activating it.");
            gameObject.SetActive(true);
        }

        StopAllCoroutines();
        StartCoroutine(ShowNotificationCoroutine(collaborators, actionName));
    }

    private IEnumerator ShowNotificationCoroutine(List<UniversalCharacterController> collaborators, string actionName)
    {
        // Activate the notification objects
        if (eurekaNotificationPanel != null)
            eurekaNotificationPanel.SetActive(true);
        if (eurekaLogPromptObject != null)
            eurekaLogPromptObject.SetActive(true);

        // Set up the notification content
        if (eurekaTitleText != null)
            eurekaTitleText.text = "EUREKA MOMENT!";
        
        string description = FormatCollaboratorNames(collaborators) + " have made a discovery.";
        if (eurekaDescriptionText != null)
            eurekaDescriptionText.text = description;
        
        if (eurekaLogPromptText != null)
            eurekaLogPromptText.text = "Press F5 to view Eureka Log";

        // Fade in
        yield return StartCoroutine(FadeCoroutine(0, 1, fadeInDuration));

        // Wait for the notification duration
        yield return new WaitForSeconds(notificationDuration);

        // Fade out
        yield return StartCoroutine(FadeCoroutine(1, 0, fadeOutDuration));

        // Hide the notification
        HideNotification();
    }

    private string FormatCollaboratorNames(List<UniversalCharacterController> collaborators)
    {
        List<string> formattedNames = new List<string>();
        foreach (var collaborator in collaborators)
        {
            if (collaborator != null)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(collaborator.characterColor);
                formattedNames.Add($"<color=#{colorHex}>{collaborator.characterName}</color>");
            }
        }

        if (formattedNames.Count == 0)
            return "Unknown collaborators";
        else if (formattedNames.Count == 1)
            return formattedNames[0];
        else if (formattedNames.Count == 2)
            return $"{formattedNames[0]} and {formattedNames[1]}";
        else
            return string.Join(", ", formattedNames.GetRange(0, formattedNames.Count - 1)) + $", and {formattedNames[formattedNames.Count - 1]}";
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        CanvasGroup notificationCanvasGroup = eurekaNotificationPanel?.GetComponent<CanvasGroup>();
        CanvasGroup logPromptCanvasGroup = eurekaLogPromptObject?.GetComponent<CanvasGroup>();

        if (notificationCanvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup component not found on EurekaNotificationPanel");
            yield break;
        }

        if (logPromptCanvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup component not found on EurekaLogPrompt object");
            yield break;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            
            notificationCanvasGroup.alpha = alpha;
            logPromptCanvasGroup.alpha = alpha;
            
            yield return null;
        }

        notificationCanvasGroup.alpha = endAlpha;
        logPromptCanvasGroup.alpha = endAlpha;
    }
}