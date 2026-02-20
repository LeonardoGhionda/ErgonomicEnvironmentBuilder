using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_RotationMode : HM_Toggle
{
    [SerializeField] PivotManager _pm;
    [SerializeField] HM_Base pivotCard;
    [SerializeField] PivotManager _pivot;


    XRGrabInteractable _target;

    bool _tp, _tr, _ts;

    // Override single choices made previously 
    override public void OnClick()
    {
        base.OnClick();
        OnStateChange(_state);
    }

    private void OnStateChange(bool state)
    {
        if (state)
        {
            _deps.selection.OnSelectionChanged += ChangeTarget;
            ChangeTarget(new VRSelectionManager.SelectionChangedArgs { selection = _deps.selection.Selected });
            _deps.handMenu.AddMenuEntries(new System.Collections.Generic.List<HM_Base>() { pivotCard }, _deps);

        }
        else
        {
            _deps.selection.OnSelectionChanged -= ChangeTarget;
            ChangeTarget(new());
            _deps.handMenu.RemoveMenuEntries(new System.Collections.Generic.List<HM_Base>() { pivotCard });
        }
    }

    public override void OnRemove()
    {
        base.OnRemove();
        OnStateChange(false);
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args) 
    {
        // Reset previous tracking 
        if (_target != null)
        {
            _target.trackPosition = _tp;
            _target.trackRotation = _tr;
            _target.trackScale = _ts;
        }

        _target = args.selection;
        _pivot.Target = _target;

        if (_target == null) return;

        // Save current tracking
        _tp = _target.trackPosition;
        _tr = _target.trackRotation;
        _ts = _target.trackScale;

        // Set rotation mode
        _target.trackPosition = false;
        _target.trackRotation = true;
        _target.trackScale = false;
    }
}
