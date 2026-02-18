using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class ImmersiveEditor : AbsAppState
{
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;
    private readonly ImmersiveEditorView _view;
    private readonly VRSelectionManager _selectionManager;
    private readonly MeasureManager _measureManager;
    private readonly HandMenuManager _handMenuManager;
    private readonly ScaleManager _scaleManager;

    private Vector3 _insideWallPosition = Vector3.zero;
    private bool _hmWaitRelease = false;
    private bool _snapEnabled = false;
    private SnapTools _snapTool;
    private Transform _leftController, _rightController;


    public ImmersiveEditor(
        StateManager manager,
        AppActions input,
        RoomBuilderManager roomBuilderManager,
        GameObject vrPlayer,
        ImmersiveEditorView view,
        VRSelectionManager selectionManager,
        MeasureManager measureManager,
        HandMenuManager handMenuManager,
        ScaleManager scaleManager) : base(manager, input)
    {
        _rbm = roomBuilderManager;
        _vrPlayer = vrPlayer;
        _view = view;
        _selectionManager = selectionManager;
        _measureManager = measureManager;
        _handMenuManager = handMenuManager;
        _scaleManager = scaleManager;

        // Get controllers from VR player
        if (_vrPlayer.TryGetComponent<XRInputModalityManager>(out var imManager))
        {
            _leftController = imManager.leftController.transform;
            _rightController = imManager.rightController.transform;
        }
        else Debug.LogError($"Missing XRInputModalityManager from Vr player");
    }

    public override void Enter()
    {
        // Set player position inside walls 
        RoomsUtility.CreateRoom(_rbm.RoomName);
        // Forece physics update to sync transforms
        Physics.SyncTransforms();
        _insideWallPosition = RoomsUtility.FindInternalPoint();
        _insideWallPosition.y = 0;
        _vrPlayer.transform.position = _insideWallPosition;

        // Input
        _input.HandMenu.Enable();
        _input.HandMenu.MoveEntries.started += MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled += MoveHandMenuEntriesReleased;
        _input.HandMenu.Confirm.performed += HandMenuConfirm;
        _input.HandMenu.Open.performed += MenuButtonClicked;

        _input.VR.Enable();
        _input.VR.Deselect.performed += DeselectPerformed;
        _input.VR.TakeMeasure.performed += TakeMeasurePerformed;
        _input.VR.CancelMeasure.performed += CancelMeasurePerformed;
        _input.VR.LeftTrigger.performed += TriggerPerformed;
        _input.VR.RightTrigger.performed += TriggerPerformed;


        _snapTool = new();

        _measureManager.Init(_vrPlayer.GetComponentInChildren<Camera>());

        _view.Init(
            new HM_Base.Dependencies
            {
                measure = _measureManager,
                player = _vrPlayer,
                rbm = _rbm,
                selection = _selectionManager,
                state = _manager,
                handMenu = _handMenuManager,
                scale = _scaleManager,
            });

        _handMenuManager.Init();
    }

    public override void Exit()
    {
        RoomsUtility.GenerateRoomPreview(Camera.main, _rbm.RoomName);
        RoomsUtility.Save(_rbm.RoomName);

        RoomsUtility.CleanupRoom();

        // Input
        _input.HandMenu.MoveEntries.started -= MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled -= MoveHandMenuEntriesReleased;
        _input.HandMenu.Confirm.performed -= HandMenuConfirm;
        _input.HandMenu.Open.performed -= MenuButtonClicked;
        _input.HandMenu.Disable();

        _input.VR.Deselect.performed -= DeselectPerformed;
        _input.VR.TakeMeasure.performed -= TakeMeasurePerformed;
        _input.VR.CancelMeasure.performed -= CancelMeasurePerformed;
        _input.VR.LeftTrigger.performed -= TriggerPerformed;
        _input.VR.RightTrigger.performed -= TriggerPerformed;
        _input.VR.Disable();

        // Selection Manager
        _measureManager.ClearAllMeasures();
        _measureManager.ResetTool();

        _handMenuManager.TurnOff();
    }

    public override void UpdateState()
    {
        if (_selectionManager.SelectionExist && _snapEnabled)
        {
            if (_snapTool.TrySnap(_selectionManager.Selected.transform))
                _selectionManager.ReleaseCurrentlySelectedObject();
        }

        if (_measureManager.IsMeasuring)
        {
#if USE_XR //change the vr/DT difference, don't use conditional compiling
            var rController = _vrPlayer.GetComponent<XRInputModalityManager>().rightController.transform;
            _measureManager.MoveCursor(rController);
#endif
            return;
        }
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

    void MoveHandMenuEntriesReleased(UnityEngine.InputSystem.InputAction.CallbackContext _) => _hmWaitRelease = false;

    void HandMenuConfirm(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _view.HandMenuActions(HandMenuInput.CONFIRM);
    }

    void MenuButtonClicked(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _handMenuManager.Toggle();
    }

    void DeselectPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _selectionManager.ReleaseCurrentlySelectedObject();
        _selectionManager.ClearSelection();
    }

    void TakeMeasurePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (_measureManager.IsMeasuring)
            _measureManager.RegisterClick();
    }

    void CancelMeasurePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (_measureManager.IsMeasuring)
            _measureManager.ResetTool();
    }

    private void TriggerPerformed(InputAction.CallbackContext context)
    {
        if (context.action == _input.VR.LeftTrigger)
        {
            _selectionManager.PeformControllerRaycast(_leftController);
        }
        else if (context.action == _input.VR.RightTrigger)
        {
            _selectionManager.PeformControllerRaycast(_rightController);
        }
        else
        {
            Debug.LogError($"Trigger performet that is neither left or right");
        }
    }
}



