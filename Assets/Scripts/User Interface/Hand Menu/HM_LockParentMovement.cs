using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_LockParentMovement : HM_Toggle
{
    private InteractableParent _target;
    private VRSelectionManager _sm;


    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = Managers.Get<VRSelectionManager>();
        _sm.OnSelectionChanged += ChangeTarget;
        ChangeTarget(new(_sm.Selected));
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        // If card parent is selected, apply the change to all siblings
        if (_target != null)
        {
            base.OnClick();
            _target.Locked = _state;
        }
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        if(args.selection == null)
        {
            _target = null;
            _state = false;
            UpdateVisual();
            return;
        }

        _target = args.selection.GetComponent<InteractableObject>().Parent;
        _state = _target.Locked;

        UpdateVisual();
    }


    private void OnDestroy()
    {
        if (_sm != null) _sm.OnSelectionChanged -= ChangeTarget;
    }
}

