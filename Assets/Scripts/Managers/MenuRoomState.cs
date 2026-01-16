using UnityEngine;

public class MenuRoomState : AbsAppState
{
    readonly private GameObject _container;
    private MenuRoomView _view;
    private RoomBuilderManager _rbm;

    public MenuRoomState(
        StateManager manager, 
        AppActions input, 
        GameObject container, 
        MenuRoomView view,
        RoomBuilderManager roomBuilderManager) : base(manager, input)
    {
        _container = container;
        _view = view;
        _rbm = roomBuilderManager;
    }

    public override void Enter()
    {
        _view.RoomCardClicked += (string roomName) => StartEdit(roomName);
        _container.SetActive(true);
    }

    public override void Exit()
    {
        _view.RoomCardClicked -= (string roomName) => StartEdit(roomName);
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
}