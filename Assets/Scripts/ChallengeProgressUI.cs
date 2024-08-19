using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeProgressUI : MonoBehaviour
{
    [SerializeField] private Slider[] milestoneProgressBars;
    [SerializeField] private TextMeshProUGUI collectiveScoreText;
    private Color unfilledColor = new Color(0x3A / 255f, 0x3A / 255f, 0x3A / 255f); // #3A3A3A

    public void Initialize(Color hubColor)
    {
        if (milestoneProgressBars == null || milestoneProgressBars.Length == 0)
        {
            Debug.LogError("Milestone Progress Bars not assigned in ChallengeProgressUI");
            return;
        }

        foreach (var slider in milestoneProgressBars)
        {
            if (slider == null) continue;

            var backgroundImage = slider.transform.Find("Background")?.GetComponent<Image>();
            if (backgroundImage != null) backgroundImage.color = unfilledColor;

            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.color = hubColor;

            slider.value = 0;
            slider.maxValue = 1;
            slider.wholeNumbers = true;
        }

        UpdateCollectiveScore(0);
    }

    public void UpdateMilestoneProgress(float[] progress)
    {
        for (int i = 0; i < milestoneProgressBars.Length && i < progress.Length; i++)
        {
            if (milestoneProgressBars[i] != null)
            {
                milestoneProgressBars[i].value = progress[i] >= 1f ? 1f : 0f;
            }
        }
    }

    public void UpdateCollectiveScore(int score)
    {
        if (collectiveScoreText != null)
        {
            collectiveScoreText.text = $"Collective Score: {score}";
        }
    }
}