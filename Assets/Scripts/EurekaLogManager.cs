using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EurekaLogManager : MonoBehaviour
{
    public static EurekaLogManager Instance { get; private set; }

    [System.Serializable]
    public class EurekaLogEntry
    {
        public string title;
        public string description;
        public List<(string name, string role)> involvedCharacters;
        public string timestamp;
    }

    private List<EurekaLogEntry> eurekaLog = new List<EurekaLogEntry>();
    private const int MaxLogEntries = 50;
    private int eurekaCounter = 0;

    public event Action<EurekaLogEntry> OnEurekaLogEntryAdded;

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

    public void AddEurekaLogEntry(string description, List<UniversalCharacterController> involvedCharacters)
{
    eurekaCounter++;
    EurekaLogEntry entry = new EurekaLogEntry
    {
        title = $"Eureka Moment #{eurekaCounter}",
        description = description,
        involvedCharacters = involvedCharacters.Select(c => (c.characterName, c.aiSettings.characterRole)).ToList(),
        timestamp = DateTime.Now.ToString("HH:mm:ss")
    };

        eurekaLog.Insert(0, entry);

        if (eurekaLog.Count > MaxLogEntries)
        {
            eurekaLog.RemoveAt(eurekaLog.Count - 1);
        }

        OnEurekaLogEntryAdded?.Invoke(entry);
    }

    public List<EurekaLogEntry> GetEurekaLog()
    {
        return new List<EurekaLogEntry>(eurekaLog);
    }
}