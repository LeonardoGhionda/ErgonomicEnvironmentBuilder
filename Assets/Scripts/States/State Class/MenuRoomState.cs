using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuRoomState : AbsAppState
{
    readonly private GameObject _container;
    private MenuRoomView _view;
    private RoomBuilderManager _rbm;
    private GameObject _vrPlayer;

    public MenuRoomState(
        StateManager manager,
        AppActions input,
        GameObject container,
        MenuRoomView view,
        RoomBuilderManager roomBuilderManager,
        GameObject vrPlayer) : base(manager, input)
    {
        _container = container;
        _view = view;
        _rbm = roomBuilderManager;
        _vrPlayer = vrPlayer;
    }

    public override void Enter()
    {

        _vrPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);  

        //View
        _view.RoomCardClicked += StartEdit;

        _container.SetActive(true);
    }


    public override void Exit()
    {

        // View
        _view.RoomCardClicked -= StartEdit;

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