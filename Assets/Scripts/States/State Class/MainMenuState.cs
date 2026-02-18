using System;
using UnityEngine.InputSystem;

public class MainMenuState : AbsAppState
{
    private MainMenuUI _view;
    private bool _actionStarted;

    private InputAction _backAction;

    // Costruttore: riceve View e Manager
    public MainMenuState(StateManager manager, AppActions input, MainMenuUI view) : base(manager, input)
    {
        _view = view;
        _backAction = _input.Ui.GoBackLong;
    }

    override public void Enter()
    {
        _actionStarted = false;
        _input.Ui.Enable();
        _backAction.started += OnGoBackLongStarted;
        _backAction.canceled += OnGoBackLongCanceled;
        _backAction.performed += OnGoBackLongPerformed;

        _view.Show();

        //event subscription
        _view.OnNewRoomClicked += GoNewRoom;
        _view.OnLoadRoomClicked += GoLoadRoom;
        _view.OnOptionsClicked += GoOption;
    }

    override public void Exit()
    {
        //event unsubription
        _view.OnNewRoomClicked -= GoNewRoom;
        _view.OnLoadRoomClicked -= GoLoadRoom;
        _view.OnOptionsClicked -= GoOption;
        _view.Hide();

        _actionStarted = false;
        _backAction.started -= OnGoBackLongStarted;
        _backAction.canceled -= OnGoBackLongCanceled;
        _backAction.performed -= OnGoBackLongPerformed;
        _input.Ui.Disable();

    }

    override public void UpdateState()
    {
        if (_actionStarted)
            _view.UpdateLoadingCircle(_backAction.GetTimeoutCompletionPercentage());
    }

    private void GoNewRoom()
    {
        _manager.ChangeState(_manager.NewRoom);
    }

    private void GoLoadRoom()
    {
        _manager.ChangeState(_manager.LoadRoom);
    }

    private void GoOption()
    {
        throw new NotImplementedException();
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
            _manager.ChangeState(null);
    }

}