using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_LockRotation : HM_Toggle
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
            _target.trackRotation = !_target.trackRotation;
        }
        else
        {
            foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                grabbable.trackRotation = !_state;
            }
        }

    }

    void ChangeTarget(XRGrabInteractable selected)
    {
        _target = selected;
    }
}


