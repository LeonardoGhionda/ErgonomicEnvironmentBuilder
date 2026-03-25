using UnityEngine;

public class HM_MainMenu : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        DependencyProvider.VRPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); //Spawn position

        var sm = Managers.Get<VRSelectionManager>();
        sm.ClearSelection();
        sm.ReleaseCurrentlySelectedObject();

        var rbm = Managers.Get<RoomBuilderManager>();
        var state = Managers.Get<StateManager>();
        var measure = Managers.Get<MeasureManager>();   
        RoomManagementTools.Save(rbm.RoomName);
        state.ChangeState(state.MenuRoom);
        measure.ClearAllMeasures();
    }
}
