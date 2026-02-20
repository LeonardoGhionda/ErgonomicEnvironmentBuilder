public class HM_DeleteSelected : HM_Base
{
    VRSelectionManager _selection;
    HandMenuManager _hand;

    public override void OnClick()
    {
        base.OnClick();

        if (_selection == null) _selection = FindAnyObjectByType<VRSelectionManager>();
        if (_hand == null) _hand = FindAnyObjectByType<HandMenuManager>(UnityEngine.FindObjectsInactive.Include);

        if (_selection.SelectionExist == false) return;
        _selection.DeleteSelected();


        _hand.Show(false);
    }
}
