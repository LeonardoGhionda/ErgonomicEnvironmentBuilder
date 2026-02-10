using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MenuRoomState : AbsAppState
{
    readonly private GameObject _container;
    private MenuRoomView _view;
    private RoomBuilderManager _rbm;
    private VRSelectionManager _selectionManager;

    private bool _hmWaitRelease = false;

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
    }

    public override void Enter()
    {

        // Input
        _input.HandMenu.Enable();
        _input.HandMenu.MoveEntries.started += MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled += MoveHandMenuEntriesRelease;
        _input.HandMenu.Confirm.performed += HandMenuConfirm;
        _input.HandMenu.Open.performed += MenuButtonClicked;

        //View
        _view.RoomCardClicked += StartEdit;
        _view.StartHandMenu();

        // Selection manager Actions
        _selectionManager.OnSelectionChanged += ObjectSelected;

        _container.SetActive(true);
    }

    public override void Exit()
    {
        // Input
        _input.HandMenu.Enable();
        _input.HandMenu.MoveEntries.started -= MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled -= MoveHandMenuEntriesRelease;
        _input.HandMenu.Confirm.performed -= HandMenuConfirm;
        _input.HandMenu.Open.performed -= MenuButtonClicked;
        _input.HandMenu.Disable();
        
        // View
        _view.RoomCardClicked -= StartEdit;

        // Selection manager Actions
        _selectionManager.OnSelectionChanged -= ObjectSelected;

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

    // Input Callbacks
    void MoveHandMenuEntries(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (_hmWaitRelease) return;

        _hmWaitRelease = true;
        float deadZone = 0.1f;
        float inputVector = ctx.ReadValue<Vector2>().x;
        if (inputVector > deadZone)
            _view.HandMenuActions(HandMenuInput.RIGHT);
        else if (inputVector < -deadZone)
            _view.HandMenuActions(HandMenuInput.LEFT);

    }

    void MoveHandMenuEntriesRelease(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _hmWaitRelease = false;
    }

    void HandMenuConfirm(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _view.HandMenuActions(HandMenuInput.CONFIRM);
    }

    void MenuButtonClicked(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _view.ToggleHandMenu();
    }

    // Selection Manager Callbacks
    void ObjectSelected(XRGrabInteractable interactable)
    {
    }

}