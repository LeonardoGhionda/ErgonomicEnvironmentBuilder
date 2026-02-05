using UnityEngine;

public class HM_P2PMeasure : HM_Base
{
    public override void OnClick()
    {
        _deps.measure.StartMeasure();
        _deps.handMenu.Show(false);
    }
}
