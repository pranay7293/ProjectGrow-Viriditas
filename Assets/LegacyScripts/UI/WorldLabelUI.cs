using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldLabelUI : MonoBehaviour
{
    // TODO: Use an object pool here.
    [SerializeField] private TMPro.TMP_Text[] labels;
    [SerializeField] private Image[] icons;

    private HashSet<WorldObjectLabel> nearbyLabels = new HashSet<WorldObjectLabel>();

    public void ShowWorldLabel(WorldObjectLabel label)
    {
        nearbyLabels.Add(label);
    }

    public void HideWorldLabel(WorldObjectLabel label)
    {
        nearbyLabels.Remove(label);
    }

    private void Awake()
    {
        HideRemainder(0);
    }

    private void LateUpdate()
    {
        // Show the first visible label arbitrarily.
        // TODO: Support multiple labels.
        // TODO: Handle alpha fading.
        int uiLabelIndex = 0;
        foreach (var worldLabel in nearbyLabels)
        {
            var uiPos = Camera.main.WorldToScreenPoint(worldLabel.transform.position);

            // Positive Z means its in front of us rather than behind us.
            if (uiPos.z > 0)
            {
                Show(uiLabelIndex++, worldLabel.LabelText, worldLabel.IconSprite, uiPos);
            }
        }
        HideRemainder(uiLabelIndex);
    }

    private void Show(int uiLabelIndex, string labelText, Sprite iconSprite, Vector3 position)
    {
        labels[uiLabelIndex].transform.parent.position = position;
        labels[uiLabelIndex].enabled = true;
        icons[uiLabelIndex].enabled = true;
        labels[uiLabelIndex].text = labelText;
        icons[uiLabelIndex].sprite = iconSprite;
    }

    private void HideRemainder(int minUiLabelIndex)
    {
        for (int i = minUiLabelIndex; i < labels.Length; i++)
        {
            labels[i].enabled = false;
            icons[i].enabled = false;
        }
    }
}
