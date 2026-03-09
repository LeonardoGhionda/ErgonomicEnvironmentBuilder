using UnityEngine;

public class HM_MainMenu : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        _deps.player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); //Spawn position
        _deps.selection.ClearSelection();
        _deps.selection.ReleaseCurrentlySelectedObject();
        RoomManagementTools.Save(_deps.rbm.RoomName);
        _deps.state.ChangeState(_deps.state.MenuRoom);
        _deps.measure.ClearAllMeasures();
    }
}
