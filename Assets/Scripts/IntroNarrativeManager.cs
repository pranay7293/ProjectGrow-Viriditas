using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class IntroNarrativeManager : MonoBehaviour
{
    public TextMeshProUGUI[] narrativeLines;
    public float fadeInDuration = 0.5f;
    public float lineDuration = 1.5f;
    public float delayBetweenLines = 0.3f;
    public float delayBeforeNextScene = 1.5f;
    public Button skipButton;

    private string[] narrativeTexts = new string[]
    {
        "Welcome to <color=#0D86F8>PROJECT GROW</color>",
        "A secret collective of the world's brightest minds,\nbrought together to solve humanity's greatest challenges.",
        "10 geniuses. 1 epic mission. <color=#0D86F8>1 LEGEND</color>.",
        "Select your hub to begin."
    };

    void Start()
    {
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = new Color(1, 1, 1, 0);
            narrativeLines[i].text = narrativeTexts[i];
        }
        skipButton.onClick.AddListener(SkipIntro);
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            if (i == 2) // The "10 geniuses. 1 epic mission. 1 LEGEND." line
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
        string[] dramaticTexts = { "10 geniuses.", "1 epic mission.", "<color=#0D86F8>1 LEGEND</color>" };
        TextMeshProUGUI lineText = narrativeLines[lineIndex];
        
        for (int i = 0; i < dramaticTexts.Length; i++)
        {
            lineText.text = dramaticTexts[i];
            yield return StartCoroutine(FadeInLine(lineIndex));
            
            if (i < dramaticTexts.Length - 1) // Don't fade out the last part
            {
                yield return StartCoroutine(FadeOutLine(lineText));
            }
        }
    }

    IEnumerator FadeOutLine(TextMeshProUGUI lineText)
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - Mathf.Clamp01(elapsedTime / (fadeInDuration / 2));
            lineText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }

    void SkipIntro()
    {
        StopAllCoroutines();
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = Color.white;
            if (i == 2) // Set the final state for the dramatic reveal line
            {
                narrativeLines[i].text = "<color=#0D86F8>1 LEGEND</color>";
            }
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