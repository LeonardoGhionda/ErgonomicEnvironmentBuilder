using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

enum LockType
{
    Horizontal,
    Vertical
}

public class HM_ConstrainMovement : HM_Toggle
{
    private XRGrabInteractable _target;
    private XRGeneralGrabTransformer _grabOptions;
    private VRSelectionManager _sm;
    private HandMenuManager _hand;

    [SerializeField] LockType Lock;



    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = Managers.Get<VRSelectionManager>();
        _sm.OnSelectionChanged += ChangeTarget;
        ChangeTarget(new(_sm.Selected));
        _hand = Managers.Get<HandMenuManager>();
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        if (_target == null) return;
        base.OnClick();
        if (_state)
        {
            _grabOptions.constrainedAxisDisplacementMode = XRGeneralGrabTransformer.ConstrainedAxisDisplacementMode.WorldAxisRelative;
            XRGeneralGrabTransformer.ManipulationAxes permitted;
            permitted = (XRGeneralGrabTransformer.ManipulationAxes) 0;
            if (Lock == LockType.Vertical) permitted |= XRGeneralGrabTransformer.ManipulationAxes.Y;
            else permitted |= XRGeneralGrabTransformer.ManipulationAxes.X | XRGeneralGrabTransformer.ManipulationAxes.Z;
            _grabOptions.permittedDisplacementAxes = permitted;

            _target.trackRotation = false;

        }
        else
        {
            _grabOptions.permittedDisplacementAxes = XRGeneralGrabTransformer.ManipulationAxes.All;
            _target.trackRotation = true;
        }

        IEnumerable<HM_ConstrainMovement> cards = _hand.Entries.OfType<HM_ConstrainMovement>();
        foreach (var card in cards) card.UpdateState();
    }


    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        if (args.selection == null)
        {
            _target = null;
            _state = false;
            UpdateVisual();
            return;
        }

        _target = args.selection;
        UpdateState();
    }

    public void UpdateState()
    {
        if (_target == null)
        {
            _state = false;
            _grabOptions = null;
        }
        else
        {
            _grabOptions = _target.GetComponent<XRGeneralGrabTransformer>();
            XRGeneralGrabTransformer.ManipulationAxes permitted = _grabOptions.permittedDisplacementAxes;

            if (Lock == LockType.Horizontal)
            {
                _state = permitted == (XRGeneralGrabTransformer.ManipulationAxes.X | XRGeneralGrabTransformer.ManipulationAxes.Z);
            }
            else
            {
                _state = permitted == XRGeneralGrabTransformer.ManipulationAxes.Y;
            }

            _state = _grabOptions.permittedDisplacementAxes != XRGeneralGrabTransformer.ManipulationAxes.All;
        }

        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (_sm != null) _sm.OnSelectionChanged -= ChangeTarget;
    }
}

