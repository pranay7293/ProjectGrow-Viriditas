using UnityEngine;
using UnityEngine.UI;

public class ChallengeProgressUI : MonoBehaviour
{
    [SerializeField] private Slider[] milestoneProgressBars;

    private Color unfilledColor = new Color(0x2A / 255f, 0x2A / 255f, 0x2A / 255f); // #2A2A2A
    private Color hubColor;

    private void Start()
    {
        InitializeSliders();
    }

    public void Initialize(Color hubColor)
    {
        this.hubColor = hubColor;
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        foreach (var slider in milestoneProgressBars)
        {
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.wholeNumbers = true;

            // Set the background color
            var backgroundImage = slider.transform.Find("Background").GetComponent<Image>();
            backgroundImage.color = unfilledColor;

            // Set the fill color to match the hub color
            var fillImage = slider.fillRect.GetComponent<Image>();
            fillImage.color = hubColor;
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