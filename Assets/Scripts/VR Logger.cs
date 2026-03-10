using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
public class VRLogger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComp;
    [SerializeField] private InputActionReference toggleLogAction;

    private bool _show = false;
    private Canvas _canvas;

    public static void Log(string message)
    {
        Debug.Log(message);
    }

    private void OnEnable()
    {
        toggleLogAction.action.Enable();
        toggleLogAction.action.performed += Toggle;
    }

    private void OnDisable()
    {
        toggleLogAction.action.performed -= Toggle;
        toggleLogAction.action.Disable();
    }

    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.enabled = _show;

        Application.logMessageReceived += OnLogMessage;
    }

    private void Show() { if (_show == false) Toggle(new InputAction.CallbackContext()); }

    private void Toggle(InputAction.CallbackContext context)
    {
        _show = !_show;
        if (_canvas != null)
        {
            _canvas.enabled = _show;
        }
    }

    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        string prefix;

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                prefix = $"<color=red>E: {condition}</color>";
                Show();
                break;
            case LogType.Warning:
                prefix = $"<color=yellow>W: {condition}</color>";
                Show();
                break;
            default:
                prefix = $"L: {condition}";
                break;
        }

        textComp.text = prefix + "\n" + textComp.text;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessage;
    }
}
#else

// Empty class for the build to prevent compiler errors on other scripts referencing it
public class VRLogger : MonoBehaviour { }
#endif