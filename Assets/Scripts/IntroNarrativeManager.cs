using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class IntroNarrativeManager : MonoBehaviour
{
    public TextMeshProUGUI[] narrativeLines;
    public float typingSpeed = 0.05f;
    public float pauseBetweenLines = 1f;
    public float fadeInDuration = 0.5f;
    public Button skipButton;

    private string[] narrativeTexts = new string[]
    {
        "Welcome to <color=#0D86F8>PROJECT GROW</color>.",
        "Secret playground for the world's brightest minds.",
        "10 geniuses. 1 epic mission. <color=#0D86F8>1 LEGEND</color>.",
        "SOLVE HUMANITY'S GREATEST CHALLENGES",
        "Select your hub to begin."
    };

    private int currentLine = 0;
    private Coroutine typingCoroutine;

    void Start()
    {
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = new Color(1, 1, 1, 0);
        }
        skipButton.onClick.AddListener(SkipIntro);
        StartCoroutine(TypeNarrative());
    }

    IEnumerator TypeNarrative()
    {
        while (currentLine < narrativeTexts.Length)
        {
            if (currentLine == 2) // The "10 geniuses. 1 epic mission. 1 LEGEND." line
            {
                yield return StartCoroutine(DramaticReveal());
            }
            else
            {
                yield return StartCoroutine(FadeInLine(currentLine));
                typingCoroutine = StartCoroutine(TypeLine(narrativeTexts[currentLine], narrativeLines[currentLine]));
                yield return typingCoroutine;
            }
            yield return new WaitForSeconds(pauseBetweenLines);
            currentLine++;
        }
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("ChallengeLobby");
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
    }

    IEnumerator TypeLine(string line, TextMeshProUGUI textComponent)
    {
        textComponent.text = "";
        bool insideColorTag = false;

        foreach (char c in line)
        {
            if (c == '<')
                insideColorTag = true;
            else if (c == '>')
                insideColorTag = false;

            textComponent.text += c;
            if (!insideColorTag)
            {
                yield return new WaitForSeconds(typingSpeed);
            }
        }
    }

    IEnumerator DramaticReveal()
    {
        string[] dramaticTexts = { "10 geniuses.", "1 epic mission.", "<color=#0D86F8>1 LEGEND</color>." };
        TextMeshProUGUI lineText = narrativeLines[2];
        lineText.color = Color.white;

        foreach (string text in dramaticTexts)
        {
            yield return StartCoroutine(TypeLine(text, lineText));
            yield return new WaitForSeconds(0.7f);
        }
    }

    void SkipIntro()
    {
        StopAllCoroutines();
        for (int i = 0; i < narrativeLines.Length; i++)
        {
            narrativeLines[i].color = Color.white;
            narrativeLines[i].text = narrativeTexts[i];
        }
        StartCoroutine(WaitAndLoadNextScene());
    }

    IEnumerator WaitAndLoadNextScene()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("ChallengeLobby");
    }
}