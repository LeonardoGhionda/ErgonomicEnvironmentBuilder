using UnityEngine;

public class HM_CenterPivot : HM_Toggle
{
    [SerializeField] PivotManager pivot;
    public override void OnClick()
    {
        base.OnClick();
        pivot.PivotAtCenter = _state;
    }

    public override void OnRemove()
    {
        base.OnRemove();
        pivot.PivotAtCenter = false;
    }
}
