using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class EurekaEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI collaboratorsText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float hoverTransitionDuration = 0.3f;

    private Color defaultColor;
    private const string fallbackHubColor = "0D86F8";
    private const string fallbackCharacterColor = "FFDD00";

    private void Awake()
    {
        defaultColor = backgroundImage.color;
    }

    public void SetEntryData(EurekaLogManager.EurekaLogEntry entry)
    {
        titleText.text = entry.title;
        descriptionText.text = entry.description;
        
        string collaboratorsString = FormatCollaborators(entry.involvedCharacters);
        collaboratorsText.text = collaboratorsString;
        
        timestampText.text = $"<color=#{fallbackCharacterColor}>{entry.timestamp}</color>";
    }

    private string FormatCollaborators(List<(string name, string role)> collaborators)
    {
        if (collaborators.Count == 0) return "";
        if (collaborators.Count == 1) return $"{collaborators[0].name} ({collaborators[0].role})";
        
        var formattedCollaborators = collaborators.Select(c => $"{c.name} ({c.role})");
        return string.Join(", ", formattedCollaborators.Take(collaborators.Count - 1)) + 
               $" and {formattedCollaborators.Last()}";
    }

    public void OnHover(bool isHovering)
    {
        Color targetColor;
        if (isHovering)
        {
            if (GameManager.Instance != null)
            {
                targetColor = GameManager.Instance.GetCurrentHubColor();
            }
            else
            {
                ColorUtility.TryParseHtmlString("#" + fallbackHubColor, out targetColor);
            }
        }
        else
        {
            targetColor = defaultColor;
        }

        backgroundImage.DOColor(targetColor, hoverTransitionDuration);
    }
}