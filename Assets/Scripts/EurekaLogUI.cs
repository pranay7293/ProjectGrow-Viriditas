using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EurekaLogUI : MonoBehaviour
{
    [SerializeField] private GameObject eurekaLogPanel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;

    private void Start()
    {
        eurekaLogPanel.SetActive(false);
    }

    public void ToggleEurekaLog()
    {
        bool isVisible = !eurekaLogPanel.activeSelf;
        eurekaLogPanel.SetActive(isVisible);
        if (isVisible)
        {
            RefreshEurekaLog();
        }
    }

    public bool IsLogVisible()
    {
        return eurekaLogPanel.activeSelf;
    }

    private void RefreshEurekaLog()
    {
        ClearEntries();
        List<EurekaLogManager.EurekaLogEntry> entries = EurekaLogManager.Instance.GetEurekaLog();
        
        foreach (var entry in entries)
        {
            CreateEurekaEntry(entry);
        }
    }

    private void CreateEurekaEntry(EurekaLogManager.EurekaLogEntry entry)
    {
        GameObject entryObject = Instantiate(entryPrefab, entryContainer);
        EurekaEntryUI entryUI = entryObject.GetComponent<EurekaEntryUI>();
        entryUI.SetEntryData(entry);
    }

    private void ClearEntries()
    {
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }
    }
}