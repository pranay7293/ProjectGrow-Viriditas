using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandedChallengeCard : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI challengeTitleText;
    public Button voteButton;
    public Image borderImage;
    public Image backgroundImage;
    private Button cardButton;
    private ChallengesManager challengesManager;
    private int challengeIndex;
    private bool isAvailable;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        if (borderImage == null)
            borderImage = transform.Find("Border")?.GetComponent<Image>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index, Color hubColor, bool available, bool useInvertedColors)
    {
        descriptionText.text = data.description;
        SetFormattedChallengeTitle(data.title);
        challengesManager = manager;
        challengeIndex = index;
        isAvailable = available;

        cardButton.onClick.AddListener(CollapseChallenge);
        voteButton.onClick.AddListener(VoteForChallenge);

        // Apply colors based on useInvertedColors flag
    if (useInvertedColors)
    {
        backgroundImage.color = Color.white;
        descriptionText.color = Color.black;
        challengeTitleText.color = Color.white;
        if (borderImage != null)
            borderImage.color = Color.black;
    }
    else
    {
        backgroundImage.color = hubColor;
        descriptionText.color = Color.white;  // Always set to white
        challengeTitleText.color = Color.white;  // Always set to white
        if (borderImage != null)
            borderImage.color = Color.white;
    }

        // Enable or disable the vote button based on availability
        voteButton.interactable = isAvailable;
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
        if (isAvailable)
        {
            challengesManager.OnChallengeSelected(challengeIndex);
        }
    }

    private void CollapseChallenge()
    {
        challengesManager.CollapseChallenge();
    }

    private Color GetContrastingTextColor(Color backgroundColor)
{
    // Calculate perceived brightness
    float brightness = (backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f);
    
    // Adjust the threshold for yellow and other bright colors
    float threshold = 0.65f;  // Increased from the typical 0.5f

    return brightness > threshold ? Color.black : Color.white;
}
}