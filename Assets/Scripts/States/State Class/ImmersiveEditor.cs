using UnityEngine;

public class ImmersiveEditor : AbsAppState
{
    private RoomBuilderManager _rbm;
    private GameObject _vrPlayer;

    private Vector3 _insideWallPosition = Vector3.zero;

    public ImmersiveEditor(
        StateManager manager, 
        AppActions input,
        RoomBuilderManager roomBuilderManager,
        GameObject vrPlayer) : base(manager, input)
    {
        _rbm = roomBuilderManager;
        _vrPlayer = vrPlayer;
    }

    public override void Enter()
    {
        RoomsUtility.CreateRoom(_rbm.RoomName);
        // Forece physics update to sync transforms
        Physics.SyncTransforms();
        _insideWallPosition = RoomsUtility.FindInternalPoint();
        _vrPlayer.transform.position = _insideWallPosition;

    }

    public override void Exit()
    {
        RoomsUtility.GenerateRoomPreview(Camera.main, _rbm.RoomName);
        RoomsUtility.SaveRoom(_rbm.RoomName, _rbm, true);
    }

    public override void UpdateState()
    {
    }
}
