using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using System.Text;

public class IntroNarrativeManager : MonoBehaviour
{
    [Header("Evolution Texts")]
    public TextMeshProUGUI oldProjectText;
    public TextMeshProUGUI newProjectText;
    public Image arrowImage;

    [Header("Contextual Line")]
    public TextMeshProUGUI contextualLine;

    [Header("Select Hub")]
    public TextMeshProUGUI selectHubText;

    [Header("Skip Button")]
    public Button skipButton;

    [Header("Animation Settings")]
    public float transitionDuration = 2.5f;
    public float delayBetweenAnimations = 0.5f;
    public float contextualLineDuration = 0.3f; // Faster duration for contextual lines
    public float contextualLineDelay = 0.1f; // Shorter delay between contextual lines

    [Header("Color Settings")]
    public Color normalColor = Color.white;
    public Color dimmedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color highlightColor = new Color(1f, 1f, 1f, 1f);

    private bool isSkipped = false;
    private string[] contextualTexts = { "10 GENIUSES", "1 EPIC MISSION", "1 LEGEND" };

    void Start()
    {
        InitializeUI();
        skipButton.onClick.AddListener(SkipIntro);
        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && !isSkipped)
        {
            SkipIntro();
        }
    }

    void InitializeUI()
    {
        oldProjectText.text = "LOS ALAMOS";
        newProjectText.text = "MANHATTAN PROJECT";
        SetElementsAlpha(0);
        skipButton.interactable = false;
    }

    void SetElementsAlpha(float alpha)
    {
        oldProjectText.alpha = alpha;
        newProjectText.alpha = alpha;
        arrowImage.color = new Color(1, 1, 1, alpha);
        contextualLine.alpha = alpha;
        selectHubText.alpha = alpha;
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(0.5f);
        skipButton.interactable = true;

        yield return StartCoroutine(FadeInElement(oldProjectText, transitionDuration / 2));
        yield return StartCoroutine(FadeInElement(arrowImage, transitionDuration / 2));
        yield return StartCoroutine(FadeInElement(newProjectText, transitionDuration / 2));

        yield return new WaitForSeconds(delayBetweenAnimations);

        yield return StartCoroutine(TransitionEvolutionTexts());

        yield return new WaitForSeconds(delayBetweenAnimations);

        yield return StartCoroutine(ContextualLinesReveal());

        yield return StartCoroutine(FadeInElement(selectHubText, transitionDuration / 2));
        yield return StartCoroutine(ChangeTextColor(selectHubText, highlightColor));

        yield return new WaitForSeconds(delayBetweenAnimations);

        if (!isSkipped)
        {
            StartCoroutine(FadeOutAndLoadNextScene());
        }
    }

    IEnumerator TransitionEvolutionTexts()
    {
        yield return StartCoroutine(ShuffleAndTransitionText(oldProjectText, "LOS ALAMOS", "KARYO", 5));
        yield return StartCoroutine(ShuffleAndTransitionText(newProjectText, "MANHATTAN PROJECT", "PROJECT GROW", 12));
    }

    IEnumerator ShuffleAndTransitionText(TextMeshProUGUI textElement, string originalText, string targetText, int targetLength)
    {
        float shuffleDuration = transitionDuration / 2;
        int shuffleSteps = 20;

        for (int step = 0; step < shuffleSteps; step++)
        {
            StringBuilder shuffled = new StringBuilder(targetLength);
            for (int i = 0; i < targetLength; i++)
            {
                shuffled.Append((char)Random.Range(65, 91));
            }
            textElement.text = shuffled.ToString();
            yield return new WaitForSeconds(shuffleDuration / shuffleSteps);
        }

        textElement.text = targetText;
    }

    IEnumerator ContextualLinesReveal()
    {
        for (int i = 0; i < contextualTexts.Length; i++)
        {
            contextualLine.text = contextualTexts[i];
            yield return StartCoroutine(FadeInElement(contextualLine, contextualLineDuration / 2));
            
            if (i == contextualTexts.Length - 1) // For "1 LEGEND"
            {
                Color targetColor = new Color(0.05f, 0.53f, 0.97f); // #0D86F8
                yield return contextualLine.DOColor(targetColor, contextualLineDuration).WaitForCompletion();
            }
            else
            {
                yield return new WaitForSeconds(contextualLineDuration);
                yield return StartCoroutine(FadeOutElement(contextualLine, contextualLineDuration / 2));
                yield return new WaitForSeconds(contextualLineDelay);
            }
        }
    }

    IEnumerator FadeInElement(TextMeshProUGUI element, float duration)
    {
        yield return element.DOFade(1, duration).WaitForCompletion();
    }

    IEnumerator FadeInElement(Image element, float duration)
    {
        yield return element.DOFade(1, duration).WaitForCompletion();
    }

    IEnumerator FadeOutElement(TextMeshProUGUI element, float duration)
    {
        yield return element.DOFade(0, duration).WaitForCompletion();
    }

    IEnumerator ChangeTextColor(TextMeshProUGUI element, Color targetColor)
    {
        yield return element.DOColor(targetColor, transitionDuration / 2).WaitForCompletion();
    }

    void SkipIntro()
    {
        if (isSkipped) return;
        isSkipped = true;

        StopAllCoroutines();

        oldProjectText.text = "KARYO";
        newProjectText.text = "PROJECT GROW";
        arrowImage.color = Color.white;
        contextualLine.text = "1 LEGEND";
        contextualLine.alpha = 1;
        contextualLine.color = new Color(0.05f, 0.53f, 0.97f); // #0D86F8
        selectHubText.alpha = 1;
        selectHubText.color = highlightColor;

        StartCoroutine(FadeOutAndLoadNextScene());
    }

    IEnumerator FadeOutAndLoadNextScene()
    {
        Sequence fadeOutSequence = DOTween.Sequence();
        fadeOutSequence.Join(oldProjectText.DOFade(0, transitionDuration / 2));
        fadeOutSequence.Join(newProjectText.DOFade(0, transitionDuration / 2));
        fadeOutSequence.Join(arrowImage.DOFade(0, transitionDuration / 2));
        fadeOutSequence.Join(contextualLine.DOFade(0, transitionDuration / 2));
        fadeOutSequence.Join(selectHubText.DOFade(0, transitionDuration / 2));
        yield return fadeOutSequence.WaitForCompletion();

        SceneManager.LoadScene("ChallengeLobby");
    }
}