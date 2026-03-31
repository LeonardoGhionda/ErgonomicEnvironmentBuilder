using UnityEngine;

public class HM_SpawnMonitor : HM_Group
{
    ScreenShareManager _screenM;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _screenM = Managers.Get<ScreenShareManager>();
    }

    public override void OnClick()
    {
        base.OnClick();

        // If the menu goes from closed to open show the screen. Hide it otherwise.
        _screenM.ChangeScreenType(_isMenuOpen? _screenM.ScreenToOpen : null);
    }
}
