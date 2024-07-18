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

// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.EventSystems;
// using TMPro;

// public class ChallengeCard : MonoBehaviour, IPointerClickHandler
// {
//     public TextMeshProUGUI challengeNameText;
//     public GameObject expandedChallengeObject;
//     public Button voteButton;
//     public Button expandedChallengeButton;

//     private ChallengesManager challengesManager;
//     private bool isExpanded = false;

//     private void Start()
//     {
//         challengesManager = FindObjectOfType<ChallengesManager>();

//         if (expandedChallengeObject != null)
//         {
//             expandedChallengeObject.SetActive(false);
//         }

//         if (voteButton != null)
//         {
//             voteButton.onClick.AddListener(OnVoteButtonClicked);
//         }

//         if (expandedChallengeButton != null)
//         {
//             expandedChallengeButton.onClick.AddListener(ToggleExpand);
//         }
//     }

//     public void SetChallengeDetails(string challengeName)
//     {
//         if (challengeNameText != null)
//         {
//             challengeNameText.text = string.IsNullOrEmpty(challengeName) ? "Challenge" : challengeName;
//         }
//     }

//     public void OnPointerClick(PointerEventData eventData)
//     {
//         ToggleExpand();
//     }

//     private void ToggleExpand()
//     {
//         isExpanded = !isExpanded;

//         if (expandedChallengeObject != null)
//         {
//             expandedChallengeObject.SetActive(isExpanded);
//         }

//         if (isExpanded)
//         {
//             challengesManager.OnChallengeExpanded(this);
//         }
//         else
//         {
//             challengesManager.OnChallengeCollapsed();
//         }
//     }

//     private void OnVoteButtonClicked()
//     {
//         int challengeIndex = transform.GetSiblingIndex();
//         challengesManager.OnChallengeSelected(challengeIndex);
//         ToggleExpand();
//     }
// }