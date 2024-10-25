using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class ChallengeSplashManager : MonoBehaviour
{
    public static ChallengeSplashManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Timing")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    public delegate void OnSplashComplete();
    public event OnSplashComplete onSplashComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ValidateComponents();
            HideVisuals();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ValidateComponents()
    {
        if (backgroundImage == null)
            Debug.LogError("Background Image not assigned in ChallengeSplashManager");
        if (titleText == null)
            Debug.LogError("Title Text not assigned in ChallengeSplashManager");
    }

    private void HideVisuals()
    {
        if (background) background.SetActive(false);
        if (titleText) titleText.alpha = 0;
    }

    public void DisplayChallengeSplash(string challengeTitle, Color hubColor)
    {
        // Debug.Log($"Displaying splash for: {challengeTitle} with color: {hubColor}");
        
        // Ensure components are ready
        if (backgroundImage == null || titleText == null)
        {
            Debug.LogError("Required components missing in ChallengeSplashManager");
            return;
        }

        // Set background color
        backgroundImage.color = hubColor;
        
        // Format the title into two lines if it contains a space
        string formattedTitle = FormatTitleText(challengeTitle);
        
        // Set and verify title text
        titleText.text = formattedTitle;
        // Debug.Log($"Set title text to: {titleText.text}");
        
        // Configure text properties
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.alpha = 1f;
        titleText.color = Color.white;
        
        // Start display sequence
        StartCoroutine(DisplaySplashCoroutine());
    }

    private string FormatTitleText(string title)
    {
        // Split the title at the first space and put it on two lines
        string[] words = title.Split(' ');
        if (words.Length > 1)
        {
            string firstLine = words[0];
            string remainingWords = string.Join(" ", words, 1, words.Length - 1);
            return $"{firstLine}\n{remainingWords}".ToUpper();
        }
        return title.ToUpper();
    }

    private IEnumerator DisplaySplashCoroutine()
    {
        // Show everything
        background.SetActive(true);
        titleText.gameObject.SetActive(true);
        
        // Fade in
        titleText.alpha = 0;
        var fadeInTween = DOTween.To(() => titleText.alpha, 
            x => titleText.alpha = x, 1f, fadeDuration);
        yield return fadeInTween.WaitForCompletion();

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        var fadeOutTween = DOTween.To(() => titleText.alpha, 
            x => titleText.alpha = x, 0f, fadeDuration);
        yield return fadeOutTween.WaitForCompletion();

        HideVisuals();
        onSplashComplete?.Invoke();
    }
}