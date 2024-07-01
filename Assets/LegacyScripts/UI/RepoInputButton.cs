using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RepoInputButton : MonoBehaviour
{
    [SerializeField] private ModuleInput input;

    // change the label and button assignment to match the input assigned in the inspector
    private void Awake()
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();

        if (label == null)
            Debug.LogError("RepoInputButton has no child with a TextMeshProUGUI component.");
        else
            label.text = input.name;
    }

    public void ButtonClicked()
    {
        Karyo_GameCore.Instance.uiManager.geneticDesignWindow.InputSelected(input);
    }
}
