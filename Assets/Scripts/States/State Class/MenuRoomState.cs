using UnityEngine;

public class MenuRoomState : AbsAppState
{
    readonly private GameObject _container;
    private readonly MenuRoomView _view;
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;

    public MenuRoomState(
        StateManager manager,
        AppActions input,
        MenuRoomView view
        ) : base(manager, input)
    {
        _container = DependencyProvider.MenuRoom;
        _vrPlayer = DependencyProvider.VRPlayer;
        _rbm = Managers.Get<RoomBuilderManager>();
        _view = view;
    }

    public override void Enter()
    {
        _vrPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        //View
        _view.EditRoomCardClicked += StartEdit;
        _view.TestRoomCardClicked += StartTest;

        _container.SetActive(true);
    }


    public override void Exit()
    {

        // View
        _view.EditRoomCardClicked -= StartEdit;
        _view.TestRoomCardClicked -= StartTest;

        _container.SetActive(false);
    }

    public override void UpdateState()
    {
    }

    void StartEdit(string roomName)
    {
        _rbm.RoomName = roomName;
        _manager.ChangeState(_manager.ImmersiveEditor);
    }

    void StartTest(string roomName)
    {
        _rbm.RoomName = roomName;

        _container.SetActive(false);
        _manager.ChangeStateInNewScene(_manager.TestRoom, StateManager.SceneName.Simulation);
    }
}