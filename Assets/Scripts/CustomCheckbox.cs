using UnityEngine;
using UnityEngine.UI;

public class CustomCheckbox : MonoBehaviour
{
    [SerializeField] private Image checkboxBackground;
    [SerializeField] private Image checkmarkIcon;
    [SerializeField] private Color checkedColor = Color.white;
    [SerializeField] private Color uncheckedColor = Color.gray;

    private bool isChecked = false;

    public bool IsChecked
    {
        get { return isChecked; }
        set
        {
            isChecked = value;
            UpdateVisuals();
        }
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (checkboxBackground != null)
        {
            checkboxBackground.color = isChecked ? checkedColor : uncheckedColor;
        }

        if (checkmarkIcon != null)
        {
            checkmarkIcon.gameObject.SetActive(isChecked);
        }
    }

    public void ToggleCheckbox()
    {
        IsChecked = !IsChecked;
    }
}