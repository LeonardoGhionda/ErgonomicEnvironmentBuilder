using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_SnapClosest : HM_Base
{
    private XRGrabInteractable _target;
    private SnapTools _snapTools;


    protected override void OnInitialized()
    {
        base.OnInitialized();
        Managers.Get<VRSelectionManager>().OnSelectionChanged += ChangeTarget;
        _snapTools = new SnapTools();
    }

    public override void OnClick()
    {
        if (_target == null) return;

        base.OnClick();


    }

    private void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        _target = args.selection;
    }
}
