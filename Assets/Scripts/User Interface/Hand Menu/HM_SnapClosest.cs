using System;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_SnapClosest : HM_Base
{
    private XRGrabInteractable _target;
    private SnapTools _snapTools;


    protected override void OnInitialized()
    {
        base.OnInitialized();
        _deps.selection.OnSelectionChanged += ChangeTarget;
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
