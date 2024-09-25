using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using System.Text;

public class IntroNarrativeManager : MonoBehaviour
{
    [Header("Main Message")]
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI wordText;

    [Header("Select Hub")]
    public TextMeshProUGUI selectHubText;

    [Header("Skip Button")]
    public Button skipButton;

    [Header("Animation Settings")]
    public float messageDuration = 2.5f;
    public float delayBetweenMessages = 1f;
    public float selectHubFadeDuration = 1f;
    public float finalFadeOutDuration = 1.25f;
    public float characterFadeDuration = 0.25f;

    [Header("Scramble Settings")]
    public string wideScrambleChars = "ABCDEFGHKLMNOPQRSTUVWXYZ";
    public string narrowScrambleChars = "IJ";

    private bool isSkipped = false;
    private (string number, string word)[] messages = { ("10", "GENIUSES"), ("1", "MOONSHOT"), ("1", "LEGEND") };

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
        numberText.text = "";
        wordText.text = "";
        selectHubText.alpha = 0;
        skipButton.interactable = false;
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(0.5f);
        skipButton.interactable = true;

        foreach (var message in messages)
        {
            numberText.text = message.number;
            yield return StartCoroutine(ScrambleReveal(message.word));
            yield return new WaitForSeconds(delayBetweenMessages);
        }

        yield return StartCoroutine(FadeInElement(selectHubText, selectHubFadeDuration));

        if (!isSkipped)
        {
            StartCoroutine(FadeOutAndLoadNextScene());
        }
    }

    IEnumerator ScrambleReveal(string targetWord)
    {
        int[] changeCount = new int[targetWord.Length];
        char[][] scrambleSequence = new char[targetWord.Length][];

        // Define the number of changes and scramble sequence for each character
        for (int i = 0; i < targetWord.Length; i++)
        {
            changeCount[i] = i % 3 + 2; // 2, 3, or 4 changes in a repeating pattern
            scrambleSequence[i] = new char[changeCount[i] + 1];
            for (int j = 0; j < changeCount[i]; j++)
            {
                scrambleSequence[i][j] = GetRandomChar(targetWord[i]);
            }
            scrambleSequence[i][changeCount[i]] = targetWord[i];
        }

        float charDuration = messageDuration / targetWord.Length;

        for (float elapsed = 0; elapsed < messageDuration; elapsed += Time.deltaTime)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < targetWord.Length; i++)
            {
                float charElapsed = elapsed - (i * charDuration / 2); // Stagger start times
                if (charElapsed < 0)
                {
                    sb.Append("<color=#00000000>").Append(targetWord[i]).Append("</color>");
                }
                else if (charElapsed >= charDuration)
                {
                    sb.Append(targetWord[i]);
                }
                else
                {
                    int seqIndex = Mathf.Min(changeCount[i], Mathf.FloorToInt(charElapsed / charDuration * changeCount[i]));
                    float alpha = Mathf.PingPong(charElapsed / characterFadeDuration, 1);
                    string hexAlpha = Mathf.FloorToInt(alpha * 255).ToString("X2");
                    sb.Append("<color=#FFFFFF").Append(hexAlpha).Append(">")
                      .Append(scrambleSequence[i][seqIndex])
                      .Append("</color>");
                }
            }
            wordText.text = sb.ToString();
            yield return null;
        }

        wordText.text = targetWord;
    }

    char GetRandomChar(char targetChar)
    {
        if (narrowScrambleChars.Contains(targetChar))
        {
            return narrowScrambleChars[Random.Range(0, narrowScrambleChars.Length)];
        }
        else
        {
            return wideScrambleChars[Random.Range(0, wideScrambleChars.Length)];
        }
    }

    IEnumerator FadeInElement(TextMeshProUGUI element, float duration)
    {
        yield return element.DOFade(1, duration).WaitForCompletion();
    }

    void SkipIntro()
    {
        if (isSkipped) return;
        isSkipped = true;

        StopAllCoroutines();

        var lastMessage = messages[messages.Length - 1];
        numberText.text = lastMessage.number;
        wordText.text = lastMessage.word;
        selectHubText.alpha = 1;

        StartCoroutine(FadeOutAndLoadNextScene());
    }

    IEnumerator FadeOutAndLoadNextScene()
    {
        yield return new WaitForSeconds(1f);

        Sequence fadeOutSequence = DOTween.Sequence();
        fadeOutSequence.Join(numberText.DOFade(0, finalFadeOutDuration));
        fadeOutSequence.Join(wordText.DOFade(0, finalFadeOutDuration));
        fadeOutSequence.Join(selectHubText.DOFade(0, finalFadeOutDuration));
        yield return fadeOutSequence.WaitForCompletion();

        SceneManager.LoadScene("ChallengeLobby");
    }
}