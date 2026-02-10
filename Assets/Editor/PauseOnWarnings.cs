using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PauseOnWarning
{
    static PauseOnWarning()
    {
        // Subscribe to the log message event
        Application.logMessageReceived += OnLogMessage;
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        // Check if the log is a warning
        if (type == LogType.Warning)
        {
            Debug.Break(); // This triggers the Pause button in the editor
        }
    }
}