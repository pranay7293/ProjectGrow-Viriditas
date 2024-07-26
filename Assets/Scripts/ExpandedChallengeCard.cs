using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandedChallengeCard : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public Button voteButton;
    private Button cardButton;
    private Image backgroundImage;

    private ChallengesManager challengesManager;
    private int challengeIndex;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index, Color hubColor)
    {
        descriptionText.text = data.description;
        challengesManager = manager;
        challengeIndex = index;

        cardButton.onClick.AddListener(CollapseChallenge);
        voteButton.onClick.AddListener(VoteForChallenge);

        // Apply hub color to the background
        backgroundImage.color = hubColor;

        // Adjust text color for better contrast
        descriptionText.color = GetContrastingTextColor(hubColor);
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