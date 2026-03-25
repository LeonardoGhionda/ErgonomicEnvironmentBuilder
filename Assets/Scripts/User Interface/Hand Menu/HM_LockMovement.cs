using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_LockMovement : HM_Toggle
{
    private XRGrabInteractable _target;
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
        if (_target != null)
        {
            base.OnClick();

            // Disable gravity 
            if (_target.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            //Remove snap follow
            Destroy(_target.GetComponent<SnapFollow>());

            if (_state)
            {
                _target.trackPosition = false;
                _target.trackRotation = false;
                _target.trackScale = false;
            }
            else
            {
                _target.trackPosition = true;
                _target.trackRotation = true;
                _target.trackScale = true;
            }

        }
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        _target = args.selection;
        _state = _target != null && !_target.trackPosition && !_target.trackRotation && !_target.trackScale;

        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (_sm != null) _sm.OnSelectionChanged -= ChangeTarget;
    }
}

