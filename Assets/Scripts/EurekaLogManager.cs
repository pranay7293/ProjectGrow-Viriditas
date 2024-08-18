using UnityEngine;
using System.Collections.Generic;
using System;

public class EurekaLogManager : MonoBehaviour
{
    public static EurekaLogManager Instance { get; private set; }

    [Serializable]
    public class EurekaLogEntry
    {
        public string title;
        public List<string> involvedCharacters;
        public string description;
        public string completedMilestone;
        public float timestamp;
    }

    private List<EurekaLogEntry> eurekaLog = new List<EurekaLogEntry>();

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

    public void AddEurekaLogEntry(string title, List<string> involvedCharacters, string description, string completedMilestone)
    {
        EurekaLogEntry entry = new EurekaLogEntry
        {
            title = title,
            involvedCharacters = involvedCharacters,
            description = description,
            completedMilestone = completedMilestone,
            timestamp = Time.time
        };

        eurekaLog.Add(entry);
    }

    public List<EurekaLogEntry> GetEurekaLog()
    {
        return new List<EurekaLogEntry>(eurekaLog);
    }
}