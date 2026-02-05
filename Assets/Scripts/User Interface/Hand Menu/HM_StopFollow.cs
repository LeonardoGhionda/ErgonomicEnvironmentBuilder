using UnityEngine;

public class HM_StopFollow : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        Destroy(_deps.selection.Selected.GetComponent<SnapFollow>());
        HandMenuComunication.OnStopFollow?.Invoke();
    }

}
