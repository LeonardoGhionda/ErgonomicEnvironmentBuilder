using UnityEngine;
using UnityEngine.SceneManagement;

public class HM_MainMenu : HM_Base
{
    public override void OnClick()
    {
        base.OnClick();
        
        // Reset player position
        DependencyProvider.VRPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // Relese Object
        var sm = Managers.Get<VRSelectionManager>();
        sm.ClearSelection();
        sm.ReleaseCurrentlySelectedObject();

        // Clear Measures
        var measure = Managers.Get<MeasureManager>();
        measure.ClearAllMeasures();

        // Save Room
        var rbm = Managers.Get<RoomBuilderManager>();
        RoomManagementTools.Save(rbm.RoomName);

        var state = Managers.Get<StateManager>();

        Scene activeScene = SceneManager.GetActiveScene();
        // Same scene 
        if (activeScene.name == StateManager.SceneName.Main.ToString())
        {
            state.ChangeState(state.MenuRoom);
        }
        //Different scene
        else 
        {
            state.ChangeStateInNewScene(state.MenuRoom, StateManager.SceneName.Main);
        }
    }
}
