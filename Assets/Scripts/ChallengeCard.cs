using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChallengeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI challengeTitleText;
    public Image iconImage;
    private ChallengesManager challengesManager;
    private int challengeIndex;
    private Button cardButton;
    private Image backgroundImage;
    private bool isAvailable;

    [SerializeField]
    private float hoverDarkenAmount = 0.1f;
    private Color originalColor;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
    }

    public void SetUp(ChallengeData data, ChallengesManager manager, int index, Color hubColor, bool available, Sprite icon)
    {
        challengeTitleText.text = FormatTitle(data.title);
        challengesManager = manager;
        challengeIndex = index;
        isAvailable = available;

        cardButton.onClick.AddListener(ExpandChallenge);
        cardButton.interactable = isAvailable;

        // Apply hub color to the background
        originalColor = hubColor;
        backgroundImage.color = originalColor;

        // Set the icon
        iconImage.sprite = icon;

        // Adjust text color for better contrast
        challengeTitleText.color = GetContrastingTextColor(hubColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundImage.color = DarkenColor(originalColor, hoverDarkenAmount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = originalColor;
    }

    private void ExpandChallenge()
    {
        if (isAvailable)
        {
            challengesManager.ExpandChallenge(challengeIndex);
        }
    }

    private string FormatTitle(string title)
    {
        string[] words = title.Split(' ');
        return string.Join("\n", words);
    }

    public string GetChallengeTitle()
    {
        return challengeTitleText.text.Replace("\n", " ");
    }

    private Color GetContrastingTextColor(Color backgroundColor)
    {
        float brightness = (backgroundColor.r * 299 + backgroundColor.g * 587 + backgroundColor.b * 114) / 1000;
        return brightness > 0.5f ? Color.black : Color.white;
    }

    private Color DarkenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a
        );
    }
}