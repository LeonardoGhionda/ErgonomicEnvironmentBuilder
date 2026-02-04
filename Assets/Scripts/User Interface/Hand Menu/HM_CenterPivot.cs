using UnityEngine;

public class HM_CenterPivot : HM_Toggle
{
    [SerializeField] Pivot pivot;
    public override void OnClick()
    {
        base.OnClick();
        pivot.PivotAtCenter = _state;
    }
}
