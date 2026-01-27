using Dummiesman;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ImmersiveEditor : AbsAppState
{
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;
    private readonly ImmersiveEditorView _view;
    private readonly VRSelectionManager _selectionManager;

    private Vector3 _insideWallPosition = Vector3.zero;

    private bool _hmWaitRelease = false;

    private bool _snapEnabled = false;

    private MeasureSnapTools _snapTool;


    public ImmersiveEditor(
        StateManager manager,
        AppActions input,
        RoomBuilderManager roomBuilderManager,
        GameObject vrPlayer,
        ImmersiveEditorView view,
        VRSelectionManager selectionManager) : base(manager, input)
    {
        _rbm = roomBuilderManager;
        _vrPlayer = vrPlayer;
        _view = view;
        _selectionManager = selectionManager;
    }

    public override void Enter()
    {
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

        // Selection Manager
        _selectionManager.OnSelectionChanged += ObjectSelected;

        // View
        _view.StartHandMenu();


        // Hand Menu Button
        _view.OnLockAllPositionClick += LockPosition;
        _view.OnLockAllRotationClick += LockRotation;
        _view.OnMainMenuClick += GoToMainMenu;
        _view.OnDeleteSelectedClick += DeleteSelected;
        _view.OnModelClicked += AddModel;
        _view.OnSnap += ChangeSnapState;
        _view.OnSnapNFollow += TrySnapAndFollow;
        _view.OnStopFollow += SelectedStopFollow;
        _view.OnFollow += SelectedFollowClosest;
        _view.OnGravityToggled += ApplyGravity;
        _view.OnSelectedGravity += ToggleSelectedGravity;

        _snapTool = new();
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
        _input.VR.Disable();

        // Selection Manager
        _selectionManager.OnSelectionChanged -= ObjectSelected;

        // Hand Menu Button
        _view.OnLockAllPositionClick -= LockPosition;
        _view.OnLockAllRotationClick -= LockRotation;
        _view.OnMainMenuClick -= GoToMainMenu;
        _view.OnDeleteSelectedClick -= DeleteSelected;
        _view.OnModelClicked -= AddModel;
        _view.OnSnap -= ChangeSnapState;
        _view.OnSnapNFollow -= TrySnapAndFollow;
        _view.OnStopFollow -= SelectedStopFollow;
        _view.OnFollow -= SelectedFollowClosest;
        _view.OnGravityToggled -= ApplyGravity;
        _view.OnSelectedGravity -= ToggleSelectedGravity;
    }

    public override void UpdateState()
    {
        if (_selectionManager.SelectionExist && _snapEnabled)
        {
            if (_snapTool.TrySnap(_selectionManager.Selected.transform))
                _selectionManager.ReleaseCurrentlySelectedObject();
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
        _view.ToggleHandMenu();
    }

    void DeselectPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _selectionManager.ReleaseCurrentlySelectedObject();
        _selectionManager.ClearSelection();
    }

    // Selection Manager Callbacks
    void ObjectSelected(XRGrabInteractable interactable)
    {
        _snapTool.Clear();
        if (interactable != null)
        {
            _view.AddSelectedHandMenuEntries();
        }
        else
        {
            _view.RemoveSelectedHandMenuEntries();
            ChangeSnapState(false);
        }
    }

    // Hand Menu Entry Button Click Response
    private void DeleteSelected()
    {
        _selectionManager.DeleteSelected();
    }

    private void GoToMainMenu()
    {
        _vrPlayer.transform.SetPositionAndRotation(new(0, 0, -7), Quaternion.identity); //Spawn position
        _selectionManager.ClearSelection();
        _selectionManager.ReleaseCurrentlySelectedObject();
        RoomsUtility.Save(_rbm.RoomName);
        _manager.ChangeState(_manager.MenuRoom);
    }

    private void LockPosition(bool state)
    {
        foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            grabbable.trackPosition = !state;
    }

    private void LockRotation(bool state)
    {
        foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            grabbable.trackRotation = !state;
    }

    private void AddModel(string modelFullPath)
    {
        if (string.IsNullOrEmpty(modelFullPath))
        {
            return;
        }

        OBJLoader loader = new();
        GameObject obj = loader.Load(modelFullPath);
        obj.name = $"[P] {obj.name}";
        obj.transform.SetParent(GameObject.Find("Objects Container").transform);
        Camera cam = _vrPlayer.GetComponentInChildren<Camera>();
        obj.transform.localPosition = cam.transform.position + cam.transform.forward * 4f;
        obj.AddComponent<InteractableParent>().Path = modelFullPath;

        foreach (Transform child in obj.transform)
        {
            RoomsUtility.SetUpVrObject(child, _selectionManager);
            child.AddComponent<InteractableObject>();
        }

        // Interactable parent and object are necessary to make them savable (see Rooms Utility)

    }

    private void ChangeSnapState(bool state)
    {
        if (state == false) _snapTool.Clear();
        _snapEnabled = state;
    }

    private void TrySnapAndFollow()
    {
        SelectedStopFollow();

        if (_selectionManager.SelectionExist)
            _snapTool.SnapAndFollow(_selectionManager.Selected.transform);
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
            _selectionManager.Selected.AddComponent<SnapFollow>()?.SetTarget(FindClosestToSelected(targets).transform);
    }

    private void ApplyGravity(bool state)
    {
        Rigidbody[] rbs = GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None)
            .Select(x => x.GetComponent<Rigidbody>())
            .NotNull()
            .ToArray();
        foreach (var rb in rbs)
        {
            rb.useGravity = state;
            rb.isKinematic = !state;
        }
    }

    private void ToggleSelectedGravity()
    {
        if(_selectionManager.SelectionExist)
        {
            Rigidbody rb = _selectionManager.Selected.GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.useGravity = !rb.useGravity;
                rb.isKinematic = !rb.isKinematic;
            }
        }
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



