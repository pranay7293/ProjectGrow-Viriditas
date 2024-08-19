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
    [SerializeField] private GameObject eurekaCounter;
    [SerializeField] private TextMeshProUGUI eurekaCountText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private Color unfilledColor = new Color(0x3A / 255f, 0x3A / 255f, 0x3A / 255f); // #3A3A3A
    private Color avatarRingColor = new Color(0x18 / 255f, 0x18 / 255f, 0x18 / 255f); // #181818

    public void SetPlayerInfo(string name, Color color, bool isAI, bool isLocalPlayer)
    {
        if (nameText != null) nameText.text = name;
        if (avatarRing != null) avatarRing.color = avatarRingColor;
        
        // Handle white color case (like Lilith)
        if (characterBackground != null)
        {
            characterBackground.color = color == Color.white ? unfilledColor : color;
        }
        
        if (avatarSilhouette != null) avatarSilhouette.color = Color.white;
        
        if (agentIcon != null) agentIcon.SetActive(isAI);
        if (localPlayerIcon != null) localPlayerIcon.SetActive(isLocalPlayer && !isAI);
        if (eurekaCounter != null) eurekaCounter.SetActive(!isAI);
        UpdateEurekas(0); // Initialize eurekas to zero
        UpdateScore(0); // Initialize score to zero

        SetPersonalGoalSliderColors(color);
    }

    private void SetPersonalGoalSliderColors(Color fillColor)
    {
        foreach (var slider in personalGoalSliders)
        {
            if (slider != null)
            {
                var backgroundImage = slider.transform.Find("Background")?.GetComponent<Image>();
                if (backgroundImage != null) backgroundImage.color = unfilledColor;

                var fillImage = slider.fillRect?.GetComponent<Image>();
                if (fillImage != null) fillImage.color = fillColor;

                slider.value = 0;
                slider.maxValue = 1;
                slider.wholeNumbers = true;
            }
        }
    }

    public void UpdatePersonalGoals(float[] progress)
    {
        for (int i = 0; i < personalGoalSliders.Length && i < progress.Length; i++)
        {
            if (personalGoalSliders[i] != null)
            {
                personalGoalSliders[i].value = progress[i] >= 1f ? 1f : 0f;
            }
        }
    }

    public void UpdateEurekas(int count)
    {
        if (eurekaCountText != null) 
        {
            eurekaCountText.text = count.ToString();
            if (eurekaCounter != null) eurekaCounter.SetActive(count > 0);
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
}