using Unity.Netcode;
using UnityEngine;

public class SpectatorState : AbsAppState
{
    private GameObject _player;

    public SpectatorState(
        StateManager manager,
        AppActions input, 
        GameObject DTPlayer
        ) : base(manager, input)
    {
        _player = DTPlayer;
    }

    public override void Enter()
    {
        _input.CameraMovement.Enable();

        string json = GameObject.FindAnyObjectByType<RoomBuilderManager>().RoomJson;
        RoomManagementTools.CreateSpectatorRoom(json);

        if (!NetworkManager.Singleton.IsListening)
        {
            _ = NetworkManager.Singleton.StartClient();
        }
    }

    public override void Exit()
    {
        _input.CameraMovement.Disable();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        //active player to be used in other states
        _player.SetActive(true);
    }

    public override void UpdateState()
    {
    }
}