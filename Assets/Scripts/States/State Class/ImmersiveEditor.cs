using Dummiesman;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ImmersiveEditor : AbsAppState
{
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;
    private readonly ImmersiveEditorView _view;
    private readonly VRSelectionManager _selectionManager;
    private readonly MeasureManager _measureManager;
    private readonly HandMenuManager _handMenuManager;

    private Vector3 _insideWallPosition = Vector3.zero;

    private bool _hmWaitRelease = false;

    private bool _snapEnabled = false;

    private SnapTools _snapTool;


    public ImmersiveEditor(
        StateManager manager,
        AppActions input,
        RoomBuilderManager roomBuilderManager,
        GameObject vrPlayer,
        ImmersiveEditorView view,
        VRSelectionManager selectionManager,
        MeasureManager measureManager,
        HandMenuManager handMenuManager) : base(manager, input)
    {
        _rbm = roomBuilderManager;
        _vrPlayer = vrPlayer;
        _view = view;
        _selectionManager = selectionManager;
        _measureManager = measureManager;
        _handMenuManager = handMenuManager;
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

        // Selection Manager
        _selectionManager.OnSelectionChanged += ObjectSelected;

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
                hand = _handMenuManager,
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
        _input.VR.Disable();

        // Selection Manager
        _selectionManager.OnSelectionChanged -= ObjectSelected;

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

    // Selection Manager Callbacks
    void ObjectSelected(XRGrabInteractable interactable)
    {

        if (interactable != null)
        {
            _view.OnSelected();
        }
        else
        {
            _view.OnDeselect();
            _view.OnDeselect();
        }

    }

    // Hand Menu Entry Button Click Response

    private void LockRotation(bool state)
    {
        foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            grabbable.trackRotation = !state;
    }

    

    private void ChangeSnapState(bool state)
    {
        if (state == false) _snapTool.Clear();
        _snapEnabled = state;
    }

    private void SelectedStopFollow()
    {
        if (_selectionManager.SelectionExist)
            GameObject.Destroy(_selectionManager.Selected.gameObject.GetComponent<SnapFollow>());
    }

    private void SelectedFollowClosest()
    {
        if (!_selectionManager.SelectionExist) return;

        GameObject[] targets = 
            GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None)
            .Where(x => x != _selectionManager.Selected)
            .Select(x => x.gameObject)
            .ToArray();

        if (targets.Length > 0)
        {
            var snapFollow = _selectionManager.Selected.AddComponent<SnapFollow>();
            if(snapFollow != null) snapFollow.SetTarget(FindClosestToSelected(targets).transform);
        }
    }

    private void ApplyGravity(bool state)
    {
        
    }

    private void ToggleSelectedGravity()
    {
        if(_selectionManager.SelectionExist)
        {
            if(_selectionManager.Selected.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = !rb.useGravity;
                rb.isKinematic = !rb.isKinematic;
            }
        }
    }

    private void StartP2PMeasure()
    {

    }

    //Helpers
    GameObject FindClosestToSelected(GameObject[] targets)
    {
        GameObject closest = null;
        float closestDistSqr = Mathf.Infinity;
        Vector3 currentPos = _selectionManager.Selected.transform.position;

        foreach (GameObject potentialTarget in targets)
        {
            if (potentialTarget == null) continue;

            Vector3 directionToTarget = potentialTarget.transform.position - currentPos;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistSqr)
            {
                closestDistSqr = dSqrToTarget;
                closest = potentialTarget;
            }
        }

        return closest;
    }


}



