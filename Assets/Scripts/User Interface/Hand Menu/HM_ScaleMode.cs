using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_ScaleMode : HM_Base
{
    VRSelectionManager _sm;
    HandMenuManager _handMenu;
    ScaleManager _scaleManager;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = Managers.Get<VRSelectionManager>();
        _handMenu = Managers.Get<HandMenuManager>();
        _scaleManager = Managers.Get<ScaleManager>();
    }

    public override void OnClick()
    {
        base.OnClick();
        if (_sm.SelectionExist)
        {
            _scaleManager.StartScaling(_sm.Selected.gameObject);
        }

        _sm.OnSelectionChanged += EndScaling;
        _handMenu.Show(false);
        _handMenu.Lock = true;
    }

    private void EndScaling(VRSelectionManager.SelectionChangedArgs args)
    {
        XRGrabInteractable interactable = args.selection;

        if (interactable == null)
        {
            // Save Scale 
            _scaleManager.ConfirmScale();
        }
        else
        {
            // Reset scale 
            _scaleManager.ResetScale();
        }

        _sm.OnSelectionChanged -= EndScaling;
        _handMenu.Lock = false;
    }
}
