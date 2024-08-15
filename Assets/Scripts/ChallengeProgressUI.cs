using UnityEngine;
using UnityEngine.UI;

public class ChallengeProgressUI : MonoBehaviour
{
    [SerializeField] private Slider[] milestoneProgressBars;
    private Color unfilledColor = new Color(0x2A / 255f, 0x2A / 255f, 0x2A / 255f); // #2A2A2A

    public void Initialize(Color hubColor)
    {
        foreach (var slider in milestoneProgressBars)
        {
            // Set the background color
            var backgroundImage = slider.transform.Find("Background").GetComponent<Image>();
            backgroundImage.color = unfilledColor;

            // Set the fill color to match the hub color
            var fillImage = slider.fillRect.GetComponent<Image>();
            fillImage.color = hubColor;

            // Initialize the slider value
            slider.value = 0;
            slider.maxValue = 1;
            slider.wholeNumbers = true;
        }
    }

    public void UpdateMilestoneProgress(float[] progress)
    {
        for (int i = 0; i < milestoneProgressBars.Length && i < progress.Length; i++)
        {
            milestoneProgressBars[i].value = progress[i];
        }
    }
}