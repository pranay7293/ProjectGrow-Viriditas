using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileUI : MonoBehaviour
{
    [SerializeField] private Image avatarRing;
    [SerializeField] private Image avatarSilhouette;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider overallProgressBar;
    [SerializeField] private Slider[] personalProgressBars;
    [SerializeField] private GameObject agentIcon;
    [SerializeField] private GameObject insightIcon;
    [SerializeField] private TextMeshProUGUI insightCountText;

    private Color defaultRingColor = Color.white;
    private Color localPlayerRingColor = Color.yellow;
    private Color defaultBarColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey

    public void SetPlayerInfo(string name, Color color, bool isAI, bool isLocalPlayer)
    {
        nameText.text = name;
        avatarSilhouette.color = color;
        agentIcon.SetActive(isAI);
        insightIcon.SetActive(!isAI);
        
        SetAvatarRingColor(isLocalPlayer);
        SetProgressBarColors(color);
    }

    private void SetAvatarRingColor(bool isLocalPlayer)
    {
        avatarRing.color = isLocalPlayer ? localPlayerRingColor : defaultRingColor;
    }

    private void SetProgressBarColors(Color fillColor)
    {
        SetSliderColors(overallProgressBar, fillColor);
        foreach (var bar in personalProgressBars)
        {
            SetSliderColors(bar, fillColor);
        }
    }

    private void SetSliderColors(Slider slider, Color fillColor)
    {
        var colors = slider.colors;
        colors.disabledColor = defaultBarColor;
        colors.normalColor = fillColor;
        slider.colors = colors;
    }

    public void UpdateProgress(float overallProgress, float[] personalProgress)
    {
        overallProgressBar.value = overallProgress;
        for (int i = 0; i < personalProgressBars.Length && i < personalProgress.Length; i++)
        {
            personalProgressBars[i].value = personalProgress[i];
        }
    }

    public void UpdateInsights(int count)
    {
        insightCountText.text = count.ToString();
        insightIcon.SetActive(count > 0);
    }
}