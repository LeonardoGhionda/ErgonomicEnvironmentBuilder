using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuState : AbsAppState
{
    private readonly MainMenuUI _view;
    private bool _actionStarted;

    private readonly InputAction _backAction;

    private readonly SpectatorNetworkManager _sessionListener;

    // Costruttore: riceve View e Manager
    public MainMenuState(StateManager manager, AppActions input, MainMenuUI view) : base(manager, input)
    {
        _view = view;
        _backAction = _input.Ui.GoBackLong;
        _sessionListener = GameObject.FindAnyObjectByType<SpectatorNetworkManager>(FindObjectsInactive.Include);
        GameObject.FindAnyObjectByType<RoomBuilderManager>();
    }

    override public void Enter()
    {
        _actionStarted = false;
        _input.Ui.Enable();
        _backAction.started += OnGoBackLongStarted;
        _backAction.canceled += OnGoBackLongCanceled;
        _backAction.performed += OnGoBackLongPerformed;

        _view.Show();

        // Event subscription
        _view.OnNewRoomClicked += GoNewRoom;
        _view.OnLoadRoomClicked += GoLoadRoom;
        _view.OnOptionsClicked += GoOption;
        _view.OnJoinClicked += AcceptInvite;

        // Spectator mode
        _sessionListener.enabled = true;
        _sessionListener.RoomDataReceived += GoSpectator;
    }

    override public void Exit()
    {
        //event unsubription
        _view.OnNewRoomClicked -= GoNewRoom;
        _view.OnLoadRoomClicked -= GoLoadRoom;
        _view.OnOptionsClicked -= GoOption;
        _view.OnJoinClicked -= AcceptInvite;
        _view.Hide();

        _actionStarted = false;
        _backAction.started -= OnGoBackLongStarted;
        _backAction.canceled -= OnGoBackLongCanceled;
        _backAction.performed -= OnGoBackLongPerformed;
        _input.Ui.Disable();

        _sessionListener.RoomDataReceived -= GoSpectator;
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

    private void GoSpectator((string, string) roomData)
    {
        Debug.Log("going spectator");
        // Save room information
        (string path, string json) = roomData;

        var rbm = GameObject.FindAnyObjectByType<RoomBuilderManager>();
        rbm.RoomName = Path.GetFileNameWithoutExtension(path);
        rbm.RoomJson = json;

        //Change scene and state
        _manager.ChangeStateInNewScene(_manager.Spectator, StateManager.SceneName.Simulation);
    }

    private void AcceptInvite()
    {
        Debug.Log("join clicked");
        _sessionListener.AcceptInvite();
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