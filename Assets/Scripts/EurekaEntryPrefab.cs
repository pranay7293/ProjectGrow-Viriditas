using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EurekaEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI involvedCharactersText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI completedMilestoneText;
    [SerializeField] private GameObject expandedContent;
    [SerializeField] private Button expandButton;

    private bool isExpanded = false;

    private void Start()
    {
        expandButton.onClick.AddListener(ToggleExpand);
        SetExpanded(false);
    }

    public void SetEntryData(EurekaLogManager.EurekaLogEntry entry)
    {
        titleText.text = entry.title;
        timestampText.text = entry.timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        involvedCharactersText.text = string.Join(", ", entry.involvedCharacters);
        descriptionText.text = entry.description;
        completedMilestoneText.text = entry.completedMilestone;
    }

    private void ToggleExpand()
    {
        SetExpanded(!isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;
        expandedContent.SetActive(expanded);
        expandButton.transform.rotation = Quaternion.Euler(0, 0, expanded ? 180 : 0);
    }
}