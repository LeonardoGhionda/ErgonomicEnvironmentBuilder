using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RoomTestState : AbsAppState
{
    readonly private RoomBuilderManager _rbm;
    readonly private GameObject _vrPlayer;
    private readonly VRHostBroadcaster _inviteBroadcaster;

    public RoomTestState(
        StateManager manager,
        AppActions input,
        RoomBuilderManager rbm,
        GameObject vrPlayer
    ) : base(manager, input)
    {
        _rbm = rbm;
        _vrPlayer = vrPlayer;
        _inviteBroadcaster = GameObject.FindAnyObjectByType<VRHostBroadcaster>(FindObjectsInactive.Include);
    }

    public override void Enter()
    {
        // Room creation and player positioning
        RoomManagementTools.CreateTestRoom(_rbm.RoomName);
        // Forece physics update to sync transforms
        Physics.SyncTransforms();
        // Set player position inside walls 
        Vector3 insideWallPosition = RoomManagementTools.FindInternalPoint();
        insideWallPosition.y = 0;
        _vrPlayer.transform.position = insideWallPosition;

        // Lock far interaction
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = false;

        // Spectator mode
        _inviteBroadcaster.enabled = true;
        _inviteBroadcaster.StartHostingAndBroadcasting(_rbm.RoomName);
    }

    public override void Exit()
    {
        // Unlock far interaction
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = false;
    }

    public override void UpdateState()
    {

    }
}