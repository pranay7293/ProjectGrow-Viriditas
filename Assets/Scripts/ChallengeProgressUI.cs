using UnityEngine;
using UnityEngine.UI;

public class ChallengeProgressUI : MonoBehaviour
{
    [SerializeField] private Slider[] milestoneProgressBars;
    [SerializeField] private Color defaultBarColor = Color.grey;
    [SerializeField] private Color completedBarColor = new Color(0x0D / 255f, 0x86 / 255f, 0xF8 / 255f); // #0D86F8

    private void Start()
    {
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        foreach (var slider in milestoneProgressBars)
        {
            SetSliderColors(slider, defaultBarColor, completedBarColor);
        }
    }

    public void UpdateMilestoneProgress(float[] progress)
    {
        for (int i = 0; i < milestoneProgressBars.Length && i < progress.Length; i++)
        {
            milestoneProgressBars[i].value = progress[i];
            Color fillColor = progress[i] >= 1f ? completedBarColor : defaultBarColor;
            SetSliderColors(milestoneProgressBars[i], defaultBarColor, fillColor);
        }
    }

    private void SetSliderColors(Slider slider, Color backgroundColor, Color fillColor)
    {
        var colors = slider.colors;
        colors.disabledColor = backgroundColor;
        colors.normalColor = fillColor;
        slider.colors = colors;
    }
}