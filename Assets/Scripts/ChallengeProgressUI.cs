using UnityEngine;
using UnityEngine.UI;

public class ChallengeProgressUI : MonoBehaviour
{
    [SerializeField] private Slider[] milestoneProgressBars;
    private Color unfilledColor = new Color(0x3A / 255f, 0x3A / 255f, 0x3A / 255f);
    [SerializeField] private int challengeGoalScore = 1000; // New field for challenge goal score

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
            slider.wholeNumbers = false; // Changed to false for smooth progression
        }
    }

    public void UpdateMilestoneProgress(float[] progress)
    {
        for (int i = 0; i < milestoneProgressBars.Length && i < progress.Length; i++)
        {
            if (milestoneProgressBars[i] != null)
            {
                float normalizedProgress = progress[i] / challengeGoalScore;
                milestoneProgressBars[i].value = normalizedProgress;
            }
        }
    }
}