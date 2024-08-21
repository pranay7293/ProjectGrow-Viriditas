using System;
using System.Collections.Generic;
using UnityEngine;

public class EurekaLogManager : MonoBehaviour
{
    public static EurekaLogManager Instance { get; private set; }

    [Serializable]
    public class EurekaLogEntry
    {
        public string description;
        public List<string> involvedCharacters;
        public string timestamp;
    }

    private List<EurekaLogEntry> eurekaLog = new List<EurekaLogEntry>();
    private const int MaxLogEntries = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEurekaLogEntry(string description, List<string> involvedCharacters)
    {
        EurekaLogEntry entry = new EurekaLogEntry
        {
            description = description,
            involvedCharacters = involvedCharacters,
            timestamp = DateTime.Now.ToString("HH:mm:ss")
        };

        eurekaLog.Insert(0, entry); // Add new entries at the beginning

        if (eurekaLog.Count > MaxLogEntries)
        {
            eurekaLog.RemoveAt(eurekaLog.Count - 1);
        }
    }

    public List<EurekaLogEntry> GetEurekaLog()
    {
        return new List<EurekaLogEntry>(eurekaLog);
    }
}