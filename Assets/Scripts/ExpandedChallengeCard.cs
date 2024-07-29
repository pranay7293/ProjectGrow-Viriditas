using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandedChallengeCard : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI challengeTitleText;
    public Button voteButton;
    public Image borderImage;
    private Image backgroundImage;
    private Button cardButton;
    private ChallengesManager challengesManager;
    private int challengeIndex;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
        if (borderImage == null)
            borderImage = transform.Find("Border")?.GetComponent<Image>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index, Color hubColor)
    {
        descriptionText.text = data.description;
        SetFormattedChallengeTitle(data.title);
        challengesManager = manager;
        challengeIndex = index;

        cardButton.onClick.AddListener(CollapseChallenge);
        voteButton.onClick.AddListener(VoteForChallenge);

        // Apply hub color to the background
        backgroundImage.color = hubColor;

        // Ensure border is visible
        if (borderImage != null)
            borderImage.color = Color.white;

        // Adjust text color for better contrast
        descriptionText.color = GetContrastingTextColor(hubColor);
        challengeTitleText.color = GetContrastingTextColor(hubColor);
    }

    private void SetFormattedChallengeTitle(string title)
    {
        string[] words = title.Split(' ');
        if (words.Length == 2)
        {
            challengeTitleText.text = $"{words[0]}\n{words[1]}";
        }
        else if (words.Length > 2)
        {
            int midPoint = Mathf.CeilToInt(words.Length / 2f);
            string firstLine = string.Join(" ", words, 0, midPoint);
            string secondLine = string.Join(" ", words, midPoint, words.Length - midPoint);
            challengeTitleText.text = $"{firstLine}\n{secondLine}";
        }
        else
        {
            challengeTitleText.text = title;
        }
    }

    private void VoteForChallenge()
    {
        challengesManager.OnChallengeSelected(challengeIndex);
    }

    private void CollapseChallenge()
    {
        challengesManager.CollapseChallenge();
    }

    private Color GetContrastingTextColor(Color backgroundColor)
    {
        float brightness = (backgroundColor.r * 299 + backgroundColor.g * 587 + backgroundColor.b * 114) / 1000;
        return brightness > 0.5f ? Color.black : Color.white;
    }
}