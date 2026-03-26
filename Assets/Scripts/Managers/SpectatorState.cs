using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SpectatorState : AbsAppState
{
    private readonly GameObject _player;

    public SpectatorState(
        StateManager manager,
        AppActions input
        ) : base(manager, input)
    {
        _player = DependencyProvider.DTPlayer;
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

        NetworkManager.Singleton.OnClientStopped += GoMainMenu;
    }

    public override void Exit()
    {
        _input.CameraMovement.Disable();
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= GoMainMenu;

            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Destroy the old manager to prevent duplicates on scene reload
            Object.Destroy(NetworkManager.Singleton.gameObject);
        }

        _player.SetActive(true);
    }

    public override void UpdateState()
    {
    }

    private async void GoMainMenu(bool isHost)
    {
        await Task.Delay(100); // wait client shutdown operation to complete 

        var stateM = Managers.Get<StateManager>();
        stateM.ChangeStateInNewScene(stateM.MainMenu, StateManager.SceneName.Main);
    }
}