using UnityEngine;

public class HM_LockMovement : HM_Toggle
{
    private InteractableObject _target;
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
        // If no parent or parent is selected, apply the change to the current target
        if (_target != null)
        {
            base.OnClick();

            // If parent is locked, unlock it first to avoid lock state conflicts
            if (_state == false && _target.Parent.Locked)
            { 
                _target.Parent.Locked = false; 
            }

            LockTarget(_state);
        }
    }

    private void LockTarget(bool state)
    {
        // Disable gravity 
        if (_target.TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        //Remove snap follow
        Destroy(_target.GetComponent<SnapFollow>());

        _target.Locked = state;
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

        _target = args.selection.GetComponent<InteractableObject>();

        // Locked parent overrides child lock state, so if the parent is locked, the child must be locked as well
        if (_target.Parent.Locked) _target.Locked = true;
        _state = _target.Locked;

        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (_sm != null) _sm.OnSelectionChanged -= ChangeTarget;
    }
}

