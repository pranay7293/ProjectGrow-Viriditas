using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class ActTitleManager : MonoBehaviour
{
    public static ActTitleManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI actTitleText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

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

        // Ensure the Act Title text is hidden at start, but keep the GameObject active
        if (actTitleText != null)
        {
            actTitleText.alpha = 0f;
        }
    }

    public void DisplayActTitle(string actTitle)
    {
        // Ensure the GameObject is active before starting the coroutine
        gameObject.SetActive(true);
        StartCoroutine(DisplayActTitleCoroutine(actTitle));
    }

    private IEnumerator DisplayActTitleCoroutine(string actTitle)
    {
        if (actTitleText == null)
        {
            Debug.LogError("Act Title Text is not assigned in ActTitleManager");
            yield break;
        }

        actTitleText.text = actTitle;
        actTitleText.alpha = 0f;

        // Fade in
        yield return actTitleText.DOFade(1f, fadeDuration).WaitForCompletion();

        // Display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return actTitleText.DOFade(0f, fadeDuration).WaitForCompletion();

        // Keep the GameObject active, but the text is now invisible
    }
}