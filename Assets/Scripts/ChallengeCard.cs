using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeCard : MonoBehaviour
{
    public TextMeshProUGUI challengeTitleText;
    private ChallengesManager challengesManager;
    private int challengeIndex;
    private Button cardButton;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index)
    {
        challengeTitleText.text = FormatTitle(data.title);
        challengesManager = manager;
        challengeIndex = index;

        cardButton.onClick.AddListener(ExpandChallenge);
    }

    private void ExpandChallenge()
    {
        challengesManager.ExpandChallenge(challengeIndex);
    }

    private string FormatTitle(string title)
    {
        // Split the title into words
        string[] words = title.Split(' ');
        
        // Join the words with line breaks
        return string.Join("\n", words);
    }

    public string GetChallengeTitle()
    {
        return challengeTitleText.text.Replace("\n", " ");
    }
}