using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_StopFollow : HM_Toggle
{
    SnapFollow _targetComponent;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var sm = Managers.Get<VRSelectionManager>();
        sm.OnSelectionChanged += UpdateTarget;
        UpdateTarget(new(sm.Selected));
    }

    public override void OnClick()
    {
        Destroy(_targetComponent);
        UpdateTarget(new());
    }

    private void UpdateTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        XRGrabInteractable target = args.selection;

        if (target != null) _targetComponent = target.GetComponent<SnapFollow>();
        else _targetComponent = null;

        _state = _targetComponent != null;
        UpdateVisual();
    }
}
