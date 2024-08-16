using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActionLogManager : MonoBehaviour
{
    public static ActionLogManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI actionLogText;
    [SerializeField] private int maxLogEntries = 100;
    [SerializeField] private GameObject actionLogPanel;

    private Queue<string> actionLog = new Queue<string>();

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

    private void Start()
    {
        actionLogPanel.SetActive(false);
    }

    public void LogAction(string characterName, string action)
    {
        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {characterName}: {action}";
        actionLog.Enqueue(logEntry);

        if (actionLog.Count > maxLogEntries)
        {
            actionLog.Dequeue();
        }

        UpdateActionLogDisplay();
    }

    private void UpdateActionLogDisplay()
    {
        actionLogText.text = string.Join("\n", actionLog);
    }

    public void ToggleActionLog()
    {
        actionLogPanel.SetActive(!actionLogPanel.activeSelf);
        if (actionLogPanel.activeSelf)
        {
            UpdateActionLogDisplay();
        }
    }
}