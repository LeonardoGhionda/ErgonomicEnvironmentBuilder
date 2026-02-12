using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_ScaleMode : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        if(_deps.selection.SelectionExist)
        {
            _deps.scale.StartScaling(_deps.selection.Selected.gameObject);
        }

        _deps.selection.OnSelectionChanged += EndScaling;
        _deps.handMenu.Show(false);
        _deps.handMenu.Lock = true;
    }

    private void EndScaling(VRSelectionManager.SelectionChangedArgs args)
    {
        XRGrabInteractable interactable = args.selection;

        if (interactable == null)
        {
            // Save Scale 
            _deps.scale.ConfirmScale();
        }
        else
        {
            // Reset scale 
            _deps.scale.ResetScale();
        }

        _deps.selection.OnSelectionChanged -= EndScaling;
        _deps.handMenu.Lock = false;
    }
}
