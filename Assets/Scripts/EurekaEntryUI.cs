using UnityEngine;
using TMPro;

public class EurekaEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI entryText;

    public void SetEntryData(EurekaLogManager.EurekaLogEntry entry)
    {
        entryText.text = $"[{entry.timestamp}] {string.Join(", ", entry.involvedCharacters)}: {entry.description}";
    }
}
