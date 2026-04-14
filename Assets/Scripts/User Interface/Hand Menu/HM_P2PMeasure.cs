using UnityEngine;

public class HM_P2PMeasure : HM_Base
{
    [SerializeField] bool height = false;

    public override void OnClick()
    {
        if (height) Managers.Get<MeasureManager>().MeasureHeight();
        else Managers.Get<MeasureManager>().StartMeasure();

        Managers.Get<HandMenuManager>().Show(false);
    }
}
