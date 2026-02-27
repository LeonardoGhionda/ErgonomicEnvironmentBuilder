using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuRoomState : AbsAppState
{
    readonly private GameObject _container;
    private MenuRoomView _view;
    private RoomBuilderManager _rbm;
    private VRSelectionManager _selectionManager;

    private LocomotionManager _locomotionManager;

    public MenuRoomState(
        StateManager manager,
        AppActions input,
        GameObject container,
        MenuRoomView view,
        RoomBuilderManager roomBuilderManager,
        VRSelectionManager selectionManager) : base(manager, input)
    {
        _container = container;
        _view = view;
        _rbm = roomBuilderManager;
        _selectionManager = selectionManager;
        _locomotionManager = GameObject.FindAnyObjectByType<LocomotionManager>();
    }

    public override void Enter()
    {

        // Input
        _locomotionManager.LockLeftHandMovement(true);


        //View
        _view.RoomCardClicked += StartEdit;
        _view.StartHandMenu();

        _container.SetActive(true);
    }


    public override void Exit()
    {
        // Input
        _locomotionManager.LockLeftHandMovement(false);

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