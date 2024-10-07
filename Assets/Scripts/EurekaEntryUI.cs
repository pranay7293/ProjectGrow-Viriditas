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
    
    int minutes = Mathf.FloorToInt(entry.gameTime / 60f);
    int seconds = Mathf.FloorToInt(entry.gameTime % 60f);
    timestampText.text = $"<color=#{fallbackCharacterColor}>{minutes:00}:{seconds:00}</color> - {entry.actionName}";
}

    private string FormatCollaborators(List<(string name, string role, Color color)> collaborators)
    {
        if (collaborators.Count == 0) return "";
        if (collaborators.Count == 1) return FormatCollaborator(collaborators[0]);
        
        var formattedCollaborators = collaborators.Select(FormatCollaborator);
        return string.Join(", ", formattedCollaborators.Take(collaborators.Count - 1)) + 
               $" and {formattedCollaborators.Last()}";
    }

    private string FormatCollaborator((string name, string role, Color color) collaborator)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(collaborator.color);
        return $"<color=#{colorHex}>{collaborator.name}</color> ({collaborator.role})";
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