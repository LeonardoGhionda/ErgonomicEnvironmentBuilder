using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InGameLogs : MonoBehaviour
{
    public static InGameLogs Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool captureUnityLogs = true;
    [SerializeField] private int maxLines = 20;

    private TextMeshProUGUI _textMesh;
    private Queue<string> _logQueue = new Queue<string>();

    private void Awake()
    {
        // Singleton pattern setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _textMesh = GetComponent<TextMeshProUGUI>();
        _textMesh.text = "";
    }

    private void OnEnable()
    {
        if (captureUnityLogs)
            Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        if (captureUnityLogs)
            Application.logMessageReceived -= HandleUnityLog;
    }

    // --- Public API ---

    // Standard log
    public void Show(string message)
    {
        AddToQueue(message);
    }

    // Log with color
    public void Show(string message, Color color)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        AddToQueue($"<color=#{hexColor}>{message}</color>");
    }

    // --- Internal Logic ---

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";
        if (type == LogType.Error || type == LogType.Exception) color = "red";
        else if (type == LogType.Warning) color = "yellow";

        AddToQueue($"<color={color}>[{type}] {logString}</color>");
    }

    private void AddToQueue(string newMessage)
    {
        if (_logQueue.Count >= maxLines)
        {
            _logQueue.Dequeue(); // Remove oldest line
        }

        _logQueue.Enqueue(newMessage);
        UpdateText();
    }

    private void UpdateText()
    {
        _textMesh.text = string.Join("\n", _logQueue);
    }
}