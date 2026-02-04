using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static UnityEngine.GraphicsBuffer;

public class HM_LockPosition : HM_Toggle
{
    VRSelectionManager _sm;
    XRGrabInteractable _target;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = _deps.selection;
        _sm.OnSelectionChanged += ChangeTarget;
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        base.OnClick();

        if (_target != null)
        {
            _target.trackPosition = !_target.trackPosition;
        }
        else
        {
            foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                grabbable.trackPosition = !_state;
            }
        }

    }

    void ChangeTarget(XRGrabInteractable selected)
    {
        _target = selected;
    }
}
