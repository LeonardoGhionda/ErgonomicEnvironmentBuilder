using UnityEngine;

public class HM_MainMenu : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        _deps.player.transform.SetPositionAndRotation(new(0, 0, -7), Quaternion.identity); //Spawn position
        _deps.selection.ClearSelection();
        _deps.selection.ReleaseCurrentlySelectedObject();
        RoomsUtility.Save(_deps.rbm.RoomName);
        _deps.state.ChangeState(_deps.state.MenuRoom);
        _deps.measure.ClearAllMeasures();
    }
}
