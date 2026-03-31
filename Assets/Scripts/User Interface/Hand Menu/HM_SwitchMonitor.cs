using UnityEngine;

public class HM_SwitchMonitor : HM_Base
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
        _screenM.ToggleScreen();
    }
}
