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

     private void OnEnable()
    {
        EurekaLogManager.Instance.OnEurekaLogEntryAdded += OnEurekaLogEntryAdded;
    }

    private void OnDisable()
    {
        EurekaLogManager.Instance.OnEurekaLogEntryAdded -= OnEurekaLogEntryAdded;
    }

    private void OnEurekaLogEntryAdded(EurekaLogManager.EurekaLogEntry entry)
    {
        if (eurekaLogPanel.activeSelf)
        {
            CreateEurekaEntry(entry);
        }
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

     public void RefreshEurekaLog()
    {
        if (eurekaLogPanel.activeSelf)
        {
            ClearEntries();
            List<EurekaLogManager.EurekaLogEntry> entries = EurekaLogManager.Instance.GetEurekaLog();
            
            foreach (var entry in entries)
            {
                CreateEurekaEntry(entry);
            }
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