using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// only pauses on Debug.LogWarning() or Debug.LogError(), not null reference or other system exceptions 
public class DEBUG_PauseOnWarningOrError : MonoBehaviour
{
    public bool isEnabled;

    private void Awake()
    {
        Application.logMessageReceived += HandleLogMessage;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLogMessage;
    }

    private void HandleLogMessage(string logMessage, string stackTrace, LogType logType)
    {
        if (!isEnabled)
            return;

        if (logType == LogType.Warning || logType == LogType.Error)
        {
            Debug.Log("Pausing game to examine Warning or Error - disable this component to prevent this.");
            // Pause the game when a warning or error is encountered
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
#endif
        }
    }

}
