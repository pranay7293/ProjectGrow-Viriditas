using UnityEngine;
using TMPro;
using System.Collections;

public class EurekaUI : MonoBehaviour
{
    public static EurekaUI Instance { get; private set; }

    [SerializeField] private GameObject eurekaNotificationPanel;
    [SerializeField] private TextMeshProUGUI eurekaTitleText;
    [SerializeField] private TextMeshProUGUI eurekaDescriptionText;
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
    }

    public void DisplayEurekaNotification(string description)
{
    if (!gameObject.activeInHierarchy)
    {
        gameObject.SetActive(true);
    }
    StopAllCoroutines();
    StartCoroutine(ShowNotificationCoroutine(description));
}

    private IEnumerator ShowNotificationCoroutine(string description)
    {
        eurekaNotificationPanel.SetActive(true);
        eurekaTitleText.text = "EUREKA MOMENT!";
        eurekaDescriptionText.text = description;

        // Fade in
        yield return StartCoroutine(FadeTextCoroutine(0, 1, fadeInDuration));

        // Wait
        yield return new WaitForSeconds(notificationDuration);

        // Fade out
        yield return StartCoroutine(FadeTextCoroutine(1, 0, fadeOutDuration));

        eurekaNotificationPanel.SetActive(false);
    }

    private IEnumerator FadeTextCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            eurekaTitleText.alpha = alpha;
            eurekaDescriptionText.alpha = alpha;
            yield return null;
        }
    }
}