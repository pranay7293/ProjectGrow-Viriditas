using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandedChallengeCard : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public Button voteButton;
    private Button cardButton;

    private ChallengesManager challengesManager;
    private int challengeIndex;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index)
    {
        descriptionText.text = data.description;
        challengesManager = manager;
        challengeIndex = index;

        cardButton.onClick.AddListener(CollapseChallenge);
        voteButton.onClick.AddListener(VoteForChallenge);
    }

    private void VoteForChallenge()
    {
        challengesManager.OnChallengeSelected(challengeIndex);
    }

    private void CollapseChallenge()
    {
        challengesManager.CollapseChallenge();
    }
}