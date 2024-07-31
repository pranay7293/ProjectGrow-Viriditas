using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileUI : MonoBehaviour
{
    [SerializeField] private Image avatarRing;
    [SerializeField] private Image avatarSilhouette;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider overallProgressBar;
    [SerializeField] private Slider personalProgressBar;
    [SerializeField] private GameObject agentIcon;
    [SerializeField] private GameObject insightIcon;
    [SerializeField] private TextMeshProUGUI insightCountText;

    public string PlayerName { get; private set; }

    public void SetPlayerInfo(string name, Color color, bool isAI)
    {
        PlayerName = name;
        nameText.text = name;
        avatarRing.color = color;
        avatarSilhouette.color = Color.white; // Keep silhouette white for contrast
        agentIcon.SetActive(isAI);
        insightIcon.SetActive(!isAI);
    }

    public void UpdateProgress(float overallProgress, float personalProgress)
{
    if (overallProgressBar != null)
        overallProgressBar.value = overallProgress;
    if (personalProgressBar != null)
        personalProgressBar.value = personalProgress;
}

    public void UpdateInsights(int count)
    {
        insightCountText.text = count.ToString();
        insightIcon.SetActive(count > 0);
    }

    public void SetLocalPlayer(bool isLocal)
    {
        // Instead of scaling, consider using a highlight effect or border
        if (isLocal)
        {
            avatarRing.color = Color.yellow; // Example: highlight local player
        }
    }

    public void UpdateAvatarSilhouette(Sprite silhouette)
    {
        avatarSilhouette.sprite = silhouette;
    }

    // Optional: Add method to update colors dynamically
    public void UpdateColors(Color avatarColor, Color overallProgressColor, Color personalProgressColor)
    {
        avatarRing.color = avatarColor;
        overallProgressBar.fillRect.GetComponent<Image>().color = overallProgressColor;
        personalProgressBar.fillRect.GetComponent<Image>().color = personalProgressColor;
    }
}