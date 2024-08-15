using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileUI : MonoBehaviour
{
    [SerializeField] private Image avatarRing;
    [SerializeField] private Image characterBackground;
    [SerializeField] private Image avatarSilhouette;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider[] personalGoalSliders;
    [SerializeField] private GameObject agentIcon;
    [SerializeField] private GameObject localPlayerIcon;
    [SerializeField] private GameObject insightCounter;
    [SerializeField] private TextMeshProUGUI insightCountText;

    private Color unfilledColor = new Color(0x2A / 255f, 0x2A / 255f, 0x2A / 255f); // #2A2A2A
    private Color avatarRingColor = new Color(0x18 / 255f, 0x18 / 255f, 0x18 / 255f); // #181818

    public void SetPlayerInfo(string name, Color color, bool isAI, bool isLocalPlayer)
    {
        nameText.text = name;
        avatarRing.color = avatarRingColor;
        characterBackground.color = color;
        avatarSilhouette.color = Color.white;
        
        agentIcon.SetActive(isAI);
        localPlayerIcon.SetActive(isLocalPlayer && !isAI);
        insightCounter.SetActive(!isAI);

        SetPersonalGoalSliderColors(color);
    }

    private void SetPersonalGoalSliderColors(Color fillColor)
    {
        foreach (var slider in personalGoalSliders)
        {
            var backgroundImage = slider.transform.Find("Background").GetComponent<Image>();
            backgroundImage.color = unfilledColor;

            var fillImage = slider.fillRect.GetComponent<Image>();
            fillImage.color = fillColor;

            slider.value = 0;
        }
    }

    public void UpdatePersonalGoals(float[] progress)
    {
        for (int i = 0; i < personalGoalSliders.Length && i < progress.Length; i++)
        {
            personalGoalSliders[i].value = progress[i];
        }
    }

    public void UpdateInsights(int count)
    {
        insightCountText.text = count.ToString();
        insightCounter.SetActive(count > 0);
    }
}