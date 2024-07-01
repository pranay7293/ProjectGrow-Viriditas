using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RepoLogOpButton : MonoBehaviour
{
    [SerializeField] private ModuleLogicOperator logOp;
    [SerializeField] private Image logOpImage;

    // change the label and button assignment to match the input assigned in the inspector
    private void Awake()
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();

        if (label == null)
            Debug.LogError("RepoLogOpButton has no child with a TextMeshProUGUI component.");
        else
            label.text = logOp.name;

        logOpImage.sprite = logOp.image;
    }

    public void ButtonClicked()
    {
        Karyo_GameCore.Instance.uiManager.geneticDesignWindow.LogOpSelected(logOp);
    }
}
