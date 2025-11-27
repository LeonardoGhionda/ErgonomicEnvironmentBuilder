using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents a simple UI page that manages input mappings when enabled or disabled.
/// </summary>
/// <remarks>This class enables the "Ui" input map when the page is activated and disables all input mappings when
/// the page is deactivated.
public class SimpleUiPage : MonoBehaviour
{
    InputActionMap uiActionMap;

    void OnEnable()
    {
        uiActionMap = InputSystem.actions.FindActionMap("Ui");
        uiActionMap.Enable();
    }

    void OnDisable()
    {
        uiActionMap.Disable();
    }
}
