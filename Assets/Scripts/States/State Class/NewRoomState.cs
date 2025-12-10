using UnityEngine;
using System; // For Exception

public class NewRoomState : AbsAppState
{
    private NewRoomUI _view;
    private RoomBuilderManager _rbm; // Logic for the room creation geometry

    // Long press logic variables
    private LongPressData? _goBackData;
    private string _lastTriedRoomName = "";

    // Constructor
    public NewRoomState(StateManager manager, AppActions input, NewRoomUI view, RoomBuilderManager rbm)
        : base(manager, input)
    {
        _view = view;
        _rbm = rbm;
    }

    public override void Enter()
    {
        // Enable Input & View
        _input.Ui.Enable();
        _view.Show();

        // Subscribe to UI Events
        _view.OnConfirmClicked += HandleConfirm;

        // Reset Logic
        _view.SetLoadProgress(0);
        _view.ShowError("");
    }

    public override void Exit()
    {
        // Unsubscribe
        _view.OnConfirmClicked -= HandleConfirm;

        // Cleanup
        _input.Ui.Disable();
        _view.Hide();
        _goBackData = null;
    }

    public override void UpdateState()
    {
        HandleBackInput();
    }

    // --- LOGIC ---

    private void HandleBackInput()
    {
        Debug.LogWarning("TODO: Change logic to inputAction default method for holded buttons");

        var closeAction = _input.Ui.Cancel;

        // Start Press
        if (closeAction.WasPressedThisFrame())
        {
            _goBackData = LongPressedActions.RegisterAction(closeAction);
        }

        // Release Press
        if (closeAction.WasReleasedThisFrame())
        {
            _goBackData = null;
            _view.SetLoadProgress(0);
        }

        // Holding Logic
        int perc = LongPressedActions.ElapsedPercent(_goBackData, 1f);
        _view.SetLoadProgress(perc / 100f);

        if (perc >= 100)
        {
            // Action Completed: Go Back
            _goBackData = null; // Reset to avoid double trigger
            _manager.ChangeState(_manager.MainMenu); 
        }
    }

    private void HandleConfirm()
    {
        try
        {
            bool overwrite = _lastTriedRoomName == _rbm.RoomName;

            // Logic moved from old script
            RoomDataExporter.SaveRoom(_rbm.RoomName, _rbm, overwrite);
            RoomDataExporter.CreateRoom(_rbm.RoomName);

            // Update external managers if needed (legacy support)
            UiManager.Instance.RoomName = _rbm.RoomName;

            // SUCCESS: Change State to the next step
            // Assuming you have a RoomEditor state
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
}