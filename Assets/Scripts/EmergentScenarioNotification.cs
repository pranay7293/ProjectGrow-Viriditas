using UnityEngine;
using TMPro;
using System.Collections;

public class EmergentScenarioNotification : MonoBehaviour
{
    public static EmergentScenarioNotification Instance { get; private set; }

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
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
        }
        
        notificationPanel.SetActive(false);
    }

    public void DisplayNotification(string description)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowNotificationCoroutine(description));
    }

    private IEnumerator ShowNotificationCoroutine(string description)
    {
        notificationPanel.SetActive(true);
        titleText.text = "New Scenario!";
        descriptionText.text = description;

        // Fade in
        yield return StartCoroutine(FadeTextCoroutine(0, 1, fadeInDuration));

        // Wait
        yield return new WaitForSeconds(notificationDuration);

        // Fade out
        yield return StartCoroutine(FadeTextCoroutine(1, 0, fadeOutDuration));

        notificationPanel.SetActive(false);
    }

    private IEnumerator FadeTextCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            titleText.alpha = alpha;
            descriptionText.alpha = alpha;
            yield return null;
        }
    }

    public float GetNotificationDuration()
    {
        return notificationDuration + fadeInDuration + fadeOutDuration;
    }
}