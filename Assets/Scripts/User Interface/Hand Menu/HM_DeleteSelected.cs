using UnityEngine;

public class HM_DeleteSelected : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        _deps.selection.DeleteSelected();
        _deps.hand.Show(false);
    }
}
