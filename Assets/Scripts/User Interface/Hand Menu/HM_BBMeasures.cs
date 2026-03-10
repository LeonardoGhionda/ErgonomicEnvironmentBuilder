using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_BBMeasures : HM_Toggle
{
    private XRGrabInteractable _target;
    private VRSelectionManager _sm;
    private MeasureManager _mm;

    private bool _showBB = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = _deps.selection;
        _mm = _deps.measure;
        _sm.OnSelectionChanged += ChangeTarget;
        ChangeTarget(new(_sm.Selected));
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        if (_target != null)
        {
            base.OnClick();

            if (_target == null) return;
            _showBB = !_showBB;
            ChangeTarget(new(_target));
        }
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        _target = args.selection;
        _state = _target != null && _showBB;
        UpdateVisual();

        if (_state) _mm.ShowBBMeasures(_target.GetComponent<BoxCollider>());
        else _mm.HideBBMeasures();
    }

    private void OnDestroy()
    {
        if (_sm != null) _sm.OnSelectionChanged -= ChangeTarget;
        if (_mm != null) _mm.HideBBMeasures();
    }
}

