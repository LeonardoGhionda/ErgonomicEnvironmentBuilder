using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class HM_RotationMode : HM_Toggle
{
    [SerializeField] PivotManager _pm;
    [SerializeField] HM_Base pivotCard;
    XRGrabInteractable _target;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _deps.selection.OnSelectionChanged += ChangeTarget;
        ChangeTarget(new VRSelectionManager.SelectionChangedArgs { selection = _deps.selection.Selected });
    }


    // Override single choices made previously 
    override public void OnClick()
    {
        base.OnClick();

        if (_state)
        {
            _deps.handMenu.AddMenuEntries(new System.Collections.Generic.List<HM_Base>() { pivotCard }, _deps);
            _pm.Target = _target;
            foreach (var item in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                // Lock Translation
                item.trackPosition = true;
                // Lock Scale
                if (item.TryGetComponent<XRGeneralGrabTransformer>(out var scale)) scale.enabled = false;
            }
        }
        else
        {
            _deps.handMenu.RemoveMenuEntries(new System.Collections.Generic.List<HM_Base>() { pivotCard });

            _pm.Target = null;
            foreach (var item in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                // Unlock translation
                item.trackPosition = true;
                // Unlock scale 
                if (item.TryGetComponent<XRGeneralGrabTransformer>(out var scale)) scale.enabled = true;
            }
        }
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _state = true; //true because it will get changed by base.OnClick
        OnClick();
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        _target = args.selection;
        if (_state) _pm.Target = _target;
    }
}
