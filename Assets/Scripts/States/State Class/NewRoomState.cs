using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewRoomState : AbsAppState
{
    private readonly NewRoomUI _view;
    private readonly RoomBuilderManager _rbm; // Logic for the room creation geometry

    private string _lastTriedRoomName = "";

    private bool _actionStarted = false;
    private readonly InputAction _backAction;
    private readonly InputAction _moveInterface;

    private Vector2 MousePos => _input.Ui.Point.ReadValue<Vector2>();


    // Constructor
    public NewRoomState(StateManager manager, AppActions input, NewRoomUI view, RoomBuilderManager rbm)
        : base(manager, input)
    {
        _view = view;
        _rbm = rbm;
        _backAction = _input.Ui.GoBackLong;
        _moveInterface = _input.Ui.MoveInterface;
    }

    public override void Enter()
    {
        _actionStarted = false;
        // Enable Input & View
        _input.Ui.Enable();
        _backAction.started += OnGoBackLongStarted;
        _backAction.canceled += OnGoBackLongCanceled;
        _backAction.performed += OnGoBackLongPerformed;

        _moveInterface.started += MoveInterfaceStarted;
        _moveInterface.canceled += MoveInterfaceOver;

        _input.Ui.Zoom.performed += ChangeZoomFromInput;

        _view.Show();
        // Subscribe to UI Events
        _view.OnConfirmClicked += HandleConfirm;

        _view.ShowError("");

        _rbm.Init(_view, _input.Ui.Snap);
    }

    public override void Exit()
    {
        // Unsubscribe
        _view.OnConfirmClicked -= HandleConfirm;

        // Cleanup
        _actionStarted = false;

        _backAction.started -= OnGoBackLongStarted;
        _backAction.canceled -= OnGoBackLongCanceled;
        _backAction.performed -= OnGoBackLongPerformed;
        _input.Ui.Zoom.performed -= ChangeZoomFromInput;

        _moveInterface.started -= MoveInterfaceStarted;
        _moveInterface.canceled -= MoveInterfaceOver;

        _input.Ui.Disable();

        _view.Hide();
    }

    public override void UpdateState()
    {
        // Go back
        if (_actionStarted)
            HandleBackInput();

        // Move interface
        if (_moveInterface.IsInProgress())
            _view.MoveBackground(MousePos);
    }

    // --- LOGIC ---

    private void HandleBackInput()
    {
        float progressPercent = _input.Ui.GoBackLong.GetTimeoutCompletionPercentage();
        _view.UpdateLoadingCircle(progressPercent);
    }

    private void HandleConfirm()
    {
        try
        {
            bool overwrite = _lastTriedRoomName == _rbm.RoomName;

            RoomManagementTools.SaveRoom(_rbm.RoomName, _rbm, overwrite);
            RoomManagementTools.CreateDTRoom(_rbm.RoomName);


            // Change State to the next step
            _manager.ChangeState(_manager.RoomEditor);
        }

        catch (Exception e)
        {
            _view.ShowError(e.Message);

            if (e.Message == ValidationErrors.inUse)
            {
                _lastTriedRoomName = _rbm.RoomName;
            }
        }
    }

    private void OnGoBackLongStarted(InputAction.CallbackContext _)
    {
        _actionStarted = true;
    }

    private void OnGoBackLongCanceled(InputAction.CallbackContext _)
    {
        _view.UpdateLoadingCircle(0f);
        _actionStarted = false;
    }

    private void OnGoBackLongPerformed(InputAction.CallbackContext _)
    {
        if (_actionStarted)
            _manager.ChangeState(_manager.MainMenu);
    }

    private void ChangeZoomFromInput(InputAction.CallbackContext ctx) => _view.IncreaseZoomVisual(ctx.ReadValue<float>());

    private void MoveInterfaceStarted(InputAction.CallbackContext _) => _view.MoveStart(MousePos);

    private void MoveInterfaceOver(InputAction.CallbackContext _) => _view.MoveStop();
}