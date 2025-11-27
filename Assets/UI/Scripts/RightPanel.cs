using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RightPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private FreeCameraController fCam;

    private InputActionMap uiActionMap;

    private void Start()
    {
        fCam = Camera.main.GetComponent<FreeCameraController>();
        uiActionMap = InputSystem.actions.FindActionMap("Ui");

        if(uiActionMap == null)
        {
            Debug.LogError("ActionMap Ui not found");
        } 
        Visible(false);
            
    }

    /// <summary>
    /// Toggles the visibility of the current object and its direct children, and adjusts related components
    /// accordingly.
    /// </summary>
    /// <remarks>This method also updates the state of associated components, such as the camera and input
    /// manager, to reflect the visibility change.</remarks>
    /// <param name="value">A boolean indicating whether the object and its direct children should be visible. <see langword="true"/> to
    /// make them visible; otherwise, <see langword="false"/>.</param>
    /// <param name="target">The <see cref="RuntimeGizmoTransform"/> to set as the new target for the gizmoActionMap.</param>
    public void Visible(bool value, RuntimeGizmoTransform target = null)
    {
        Assert.True(value ? target != null : target == null, "When RightPanel is visible, target shouldn't be null");

        gameObject.GetComponent<Image>().enabled = value;
        foreach (Transform child in transform) // only direct children
        {
            child.gameObject.SetActive(value);
        }
        fCam.enabled = !value;
   
        if (value) uiActionMap.Enable(); else uiActionMap.Disable();

        text.text = value? target.gameObject.name : "";
        ChangeGizmoTarget(value? target : null);
    }

    /// <summary>
    /// Makes the gizmoActionMap mode changer buttons affect the given target
    /// </summary>
    /// <param name="target">Gizmo transformable object that will be affected by the ui Gizmo mode changer buttons.
    /// null disable the buttons</param>
    public void ChangeGizmoTarget(RuntimeGizmoTransform target)
    {
        //button
        var gizmoButtons = gameObject.GetComponentsInChildren<ChangeGizmoModeButton>();
        foreach (var button in gizmoButtons)
        {
            button.Target = target;
        }
        //dropdown
        var gizmoDropdown = gameObject.GetComponentInChildren<ChangeGizmoModeDropdown>(true);
        gizmoDropdown.Target = target;
    }
}
