public class HM_DeleteSelected : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        if (_deps.selection.SelectionExist == false) return;

        _deps.selection.DeleteSelected();
        _deps.handMenu.Show(false);
    }
}
