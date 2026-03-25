public class HM_P2PMeasure : HM_Base
{
    public override void OnClick()
    {
        Managers.Get<MeasureManager>().StartMeasure();
        Managers.Get<HandMenuManager>().Show(false);
    }
}
