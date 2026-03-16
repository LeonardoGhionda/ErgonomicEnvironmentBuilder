using Unity.Netcode;
using UnityEngine;

public class SpectatorState : AbsAppState
{
    public SpectatorState(
        StateManager manager,
        AppActions input
        ) : base(manager, input)
    {
    }

    public override void Enter()
    {
        _input.CameraMovement.Enable();

        string json = GameObject.FindAnyObjectByType<RoomBuilderManager>().RoomJson;
        RoomManagementTools.BuildSpectatorRoom(json);

        _ = NetworkManager.Singleton.StartClient();
    }

    public override void Exit()
    {
        _input.CameraMovement.Disable();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public override void UpdateState()
    {
    }
}