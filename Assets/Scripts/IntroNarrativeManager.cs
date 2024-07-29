using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class IntroNarrativeManager : MonoBehaviour
{
    public TextMeshProUGUI[] narrativeLines;
    public float fadeInDuration = 0.5f;
    public float lineDuration = 2f;
    public float delayBetweenLines = 0.3f;
    public float delayBeforeNextScene = 1.5f;
    public Button skipButton;

    private string[] narrativeTexts = new string[]
    {
        "Welcome to a secret collective of the world's brightest minds,\nbrought together to solve humanity's greatest challenges.",
        "1 LEGEND.",
        "Select your hub to continue."
    };

    private bool isSkipped = false;

    void Start()
    {
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = new Color(1, 1, 1, 0);
            narrativeLines[i].text = "";
        }
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

    IEnumerator PlayIntro()
    {
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            if (i == 1) // The "10 GENIUSES. 1 EPIC MISSION. 1 LEGEND." line
            {
                yield return StartCoroutine(DramaticReveal(i));
            }
            else
            {
                yield return StartCoroutine(FadeInLine(i));
            }
            yield return new WaitForSeconds(delayBetweenLines);
        }
        yield return new WaitForSeconds(delayBeforeNextScene);
        StartCoroutine(FadeOutAndLoadNextScene());
    }

    IEnumerator FadeInLine(int lineIndex)
    {
        TextMeshProUGUI lineText = narrativeLines[lineIndex];
        lineText.text = narrativeTexts[lineIndex];
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            lineText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        yield return new WaitForSeconds(lineDuration);
    }

    IEnumerator DramaticReveal(int lineIndex)
    {
        string[] dramaticTexts = { "10 GENIUSES.", "1 EPIC MISSION.", "1 LEGEND." };
        TextMeshProUGUI lineText = narrativeLines[lineIndex];
        
        for (int i = 0; i < dramaticTexts.Length; i++)
        {
            yield return StartCoroutine(FadeInText(lineText, dramaticTexts[i]));
            yield return new WaitForSeconds(lineDuration);
            
            if (i < dramaticTexts.Length - 1)
            {
                yield return StartCoroutine(FadeOutText(lineText));
            }
        }
    }

    IEnumerator FadeInText(TextMeshProUGUI lineText, string text)
    {
        lineText.text = text;
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            lineText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }

    IEnumerator FadeOutText(TextMeshProUGUI lineText)
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - Mathf.Clamp01(elapsedTime / fadeInDuration);
            lineText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        lineText.text = "";
    }

    void SkipIntro()
    {
        if (isSkipped) return;
        isSkipped = true;
        StopAllCoroutines();
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = Color.white;
            narrativeLines[i].text = narrativeTexts[i];
        }
        StartCoroutine(FadeOutAndLoadNextScene());
    }

    IEnumerator FadeOutAndLoadNextScene()
    {
        yield return StartCoroutine(FadeOutAllLines());
        SceneManager.LoadScene("ChallengeLobby");
    }

    IEnumerator FadeOutAllLines()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - Mathf.Clamp01(elapsedTime / fadeInDuration);
            foreach (TextMeshProUGUI line in narrativeLines)
            {
                line.color = new Color(1, 1, 1, alpha);
            }
            yield return null;
        }
    }
}