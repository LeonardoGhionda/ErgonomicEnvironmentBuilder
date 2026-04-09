using Dummiesman;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

using static EditorHUDView;

public class RoomEditorState : AbsAppState
{
    private readonly EditorHUDView _view;

    //managers (get from State manager)
    private readonly CameraController _camController;
    private readonly RoomBuilderManager _rbm;
    private readonly GizmoManager _gizmoManager;
    private readonly DTSelectionManager _selectionManager;
    private readonly MeasureManager _measureManager;
    private readonly ScaleManager _scaleManager;

    private Vector2 MousePos => _input.Ui.Point.ReadValue<Vector2>();

    private bool _mouseShownInPerspective = false;

    private Vector3 _insideWallPosition = Vector3.zero;

    public RoomEditorState(
        StateManager manager,
        AppActions input,
        EditorHUDView editorHUD
    ) : base(manager, input)
    {
        _view = editorHUD;
        _camController = DependencyProvider.DTCamera.GetComponent<CameraController>();
        _rbm = Managers.Get<RoomBuilderManager>();
        _gizmoManager = Managers.Get<GizmoManager>();
        _selectionManager = Managers.Get<DTSelectionManager>();
        _measureManager = Managers.Get<MeasureManager>();
        _scaleManager = Managers.Get<ScaleManager>();
    }

    public override void Enter()
    {
        // Setup UI
        _view.gameObject.SetActive(true);
        _view.ShowSelectionMenu(null);
        _view.ToggleModelsMenu(false);

        // UI events
        _view.OnSaveClicked += SaveRoom;
        _view.OnQuitClicked += QuitRoom;
        _view.OnOptionClicked += OpenOption;

        _view.OnModelButtonClicked += PlaceModel;
        _view.OnTranformButtonClicked += mode => ChangeTransformType(mode);
        _view.OnCoordinateModeChanged += button => ChangeCoordSystem(button);
        _view.OnTransformInputChanged += HandleTransformChange;
        _view.OnMeasureButtonPressed += StartMeasure;
        _view.OnClearMeasureButtonPressed += ClearMeasures;
        _view.OnImportButtonPressed += ImportModel;

        // Input events
        _input.CameraMovement.Enable();

        _input.CameraMovement.SwitchView.performed += SwitchCameraView;


        _input.Ui.GoBack.performed += OnExitPressed;
        _input.Ui.OpenModelsMenu.performed += OnToggleModels;
        _input.Ui.Select.performed += OnSelectActionPerformed;
        _input.Ui.Select.canceled += OnSelectActionCanceled;
        _input.Ui.EnablePointer.performed += OnToggleMouseRight;
        _input.Ui.Delete.performed += OnDeleteSelected;

        _input.Ui.Cancel.performed += OnCloseMenu;


        // Start Camera
        _camController.enabled = true;
        Vector3 camParams = RoomManagementTools.GetCameraParameters();
        _camController.InitOrtho(_input, new Vector3(camParams.x, 50f, camParams.y), camParams.z);

        // Ui Action always enabled
        _input.Ui.Enable();


        //start managers
        _gizmoManager.Init();
        _selectionManager.Init(_camController.Camera);
        _measureManager.Init(_camController.Camera);

        _insideWallPosition = RoomManagementTools.FindInternalPoint();
    }

    public override void Exit()
    {
        // Cleanup Events
        _view.OnSaveClicked -= SaveRoom;
        _view.OnQuitClicked -= QuitRoom;
        _view.OnOptionClicked -= OpenOption;

        _view.OnModelButtonClicked -= PlaceModel;
        _view.OnTranformButtonClicked -= mode => ChangeTransformType(mode);
        _view.OnCoordinateModeChanged -= button => ChangeCoordSystem(button);
        _view.OnTransformInputChanged -= HandleTransformChange;
        _view.OnMeasureButtonPressed -= StartMeasure;
        _view.OnClearMeasureButtonPressed -= ClearMeasures;
        _view.OnImportButtonPressed -= ImportModel;

        _input.CameraMovement.SwitchView.performed -= SwitchCameraView;

        _input.Ui.GoBack.performed -= OnExitPressed;
        _input.Ui.OpenModelsMenu.performed -= OnToggleModels;
        _input.Ui.Select.performed -= OnSelectActionPerformed;
        _input.Ui.Select.canceled -= OnSelectActionCanceled;
        _input.Ui.EnablePointer.performed -= OnToggleMouseRight;
        _input.Ui.Delete.performed -= OnDeleteSelected;

        _input.Ui.Cancel.performed -= OnCloseMenu;


        // Cleanup Maps
        _input.CameraMovement.Disable();
        _input.Ui.Disable();

        //turn off ui
        _view.gameObject.SetActive(false);

        //gizmpo manager
        _gizmoManager.Stop();

        // Move camera in position and generate a room preview
        // Preview is used in Vr menus
        _camController.SetOrtho(true);
        RoomManagementTools.GenerateRoomPreview(_rbm.RoomName);
    }

    public override void UpdateState()
    {
#if !USE_XR
        // If measuring, skip gizmo updates
        if (_measureManager.IsMeasuring)
        {
            _measureManager.MoveCursor(MousePos); 
            return;
        }
#endif
        if (_selectionManager.SelectionExist)
        {
            _gizmoManager.ScaleHandlesByCameraDistance(_selectionManager.SelectionTransform);
            _gizmoManager.HandleDragging(_selectionManager.SelectionTransform, MousePos, _input.Ui.Snap.IsPressed());
            if (_gizmoManager.SelectedMoved())
            {
                // Update HUD values during dragging
                _view.UpdateTransformUI(_selectionManager.SelectionTransform);
            }
        }
    }

    // --- INPUT HANDLERS ---

    private void SwitchCameraView(InputAction.CallbackContext context)
    {
        _camController.ToggleView();

        // When switching to perspective, this garantees player is inside the room bounds 
        if (_camController.IsOrtho == false)
        {
            _camController.Move(_insideWallPosition);
        }
    }

    private void OnSelectActionPerformed(InputAction.CallbackContext ctx)
    {
        // 1. UI PRIORITY
        // Check if pointer is hovering any UI element (Buttons, Panels, etc.)
        // If yes, stop immediately. Do not interact with 3D world.
        if (IsPointerOverUi())
        {
            return;
        }

        // 2. MEASURE PRIORITY
        if (_measureManager.IsMeasuring)
        {
            _measureManager.RegisterClick();
            return;
        }

        // 3. GIZMO PRIORITY
        // If there is a selection, check if we hit a handle first
        if (_selectionManager.SelectionExist)
        {
            if (_gizmoManager.TrySelectHandle(MousePos))
            {
                return;
            }
        }

        // 4. OBJECT SELECTION
        // No UI, No Gizmo -> Try to select an object
        // (Assuming you updated this part based on previous refactoring)
        _ = _selectionManager.Select();

        // Open/close panel
        if (_selectionManager.SelectionExist)
        {
            // Show right menu
            _view.ShowSelectionMenu(_selectionManager.SelectionGO);
            // Setup Gizmo on new selection
            _gizmoManager.NewTarget(_selectionManager.SelectionTransform);
            // Update HUD values
            _view.UpdateTransformUI(_selectionManager.SelectionTransform);
        }
        else
        {
            _view.HideAllMenus();
            _gizmoManager.RemoveGizmo();
        }

    }

    private void OnSelectActionCanceled(InputAction.CallbackContext context)
    {
        if (_selectionManager.SelectionExist)
        {
            _gizmoManager.DeselectHandle(_selectionManager.SelectionTransform);

            // When releasing the handle, if non-uniform scale was applied bake the new scale (otherwise it will cause problem in the VR profile)
            if (_gizmoManager.ObjectNonUniformScale)
            {
                GameObject selectedGO = _selectionManager.SelectionGO;
                _selectionManager.ChangeSelectedObject(null);
                _scaleManager.SetTarget(selectedGO);
                _scaleManager.ConfirmScale();
                
                _gizmoManager.ObjectNonUniformScale = false; // reset flag after applying non-uniform scale
            }
        }
    }

    private void OnToggleMouseRight(InputAction.CallbackContext ctx)
    {
        if (_gizmoManager.IsDragging) return; 
       
        _mouseShownInPerspective = !_mouseShownInPerspective;
        _camController.SetMouseFree(_mouseShownInPerspective);
    }

    private void OnExitPressed(InputAction.CallbackContext ctx)
    {
        // If measuring, cancel measuring first
        if (_measureManager.IsMeasuring)
        {
            _measureManager.CurrentStep = MeasureManager.MeasureStep.None;
            _measureManager.ResetTool();
            return;
        }

        _view.TogglePauseMenu();
        if (_view.IsPaused)
        {
            _input.CameraMovement.Disable();
            _camController.SetMouseFree(true);
        }
        else
        {
            _input.CameraMovement.Enable();
        }
    }

    private void OnToggleModels(InputAction.CallbackContext ctx)
    {
        _view.ToggleModelsMenu(true);
    }

    private void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        _view.HideAllMenus();
    }

    private void OnDeleteSelected(InputAction.CallbackContext context)
    {
        _gizmoManager.RemoveGizmo();
        _selectionManager.DeleteSelected();
        _view.HideAllMenus();
    }

    private void ChangeCoordSystem(CoordText button)
    {
        if (_selectionManager.SelectionExist)
        {
            _gizmoManager.SetLocal(!_gizmoManager.LocalTransform, _selectionManager.SelectionTransform);
            button.ChangeCoordinateMode(_gizmoManager.LocalTransform);
        }
    }


    // --- ACTIONS RESPONCE ---

    private void SaveRoom()
    {
        // Clear selection and gizmo before saving  
        _gizmoManager.RemoveGizmo();
        _selectionManager.ChangeSelectedObject(null);

        RoomManagementTools.Save(_rbm.RoomName);
    }

    private void QuitRoom()
    {
        SaveRoom();
        RoomManagementTools.CleanupRoom();
        _manager.ChangeState(_manager.MainMenu);
    }

    private void OpenOption()
    {
        SaveRoom();
        _manager.ChangeState(_manager.Option);
    }

    private void ImportModel()
    {
        ImportUtils.ImportObject(_view.GenerateModelButton);
    }

    private void ChangeTransformType(TransformMode mode)
    {
        _gizmoManager.SetMode(mode, _selectionManager.SelectionTransform);
    }

    private void StartMeasure()
    {
        _selectionManager.ChangeSelectedObject(null);
        _gizmoManager.RemoveGizmo();
        _measureManager.ResetTool();
        _view.HideAllMenus();
        _measureManager.StartMeasure();
    }

    private void ClearMeasures()
    {
        _measureManager.ClearAllMeasures();
    }

    private void PlaceModel(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        OBJLoader loader = new();
        GameObject obj = loader.FindMTLAndLoad(path);

        SetUpModel(obj, path, GameObject.Find("Objects Container"), _camController.Camera);

        _view.HideAllMenus();


        _selectionManager.ChangeSelectedObject(obj.GetComponentInChildren<InteractableParent>());
        _gizmoManager.NewTarget(_selectionManager.SelectionTransform);

        _view.ShowSelectionMenu(_selectionManager.SelectionGO);
        _view.UpdateTransformUI(_selectionManager.SelectionTransform);
    }

    public static void SetUpModel(GameObject parent, string path, GameObject container, Camera camera)
    {
        parent.name = $"[P] {parent.name}";

        parent.AddComponent<InteractableParent>().Path = path;


        MeshRenderer[] childrenMRs = parent.GetComponentsInChildren<MeshRenderer>();

        float minY = 0.0f;

        foreach (MeshRenderer mr in childrenMRs)
        {
            BoxCollider bc = mr.gameObject.AddComponent<BoxCollider>();
            float bottom = (bc.center.y - bc.size.y / 2f);
            if (bottom < minY) minY = bottom;
            _ = mr.gameObject.AddComponent<InteractableObject>();
        }

        if (camera != null)
        {
            parent.transform.position = camera.transform.position + camera.transform.forward * 5f;
            // Adjust position to sit on ground if up view is used
            if (camera.orthographic)
            {
                parent.transform.position = Vector3.Scale(parent.transform.position, new Vector3(1f, 0f, 1f));
                parent.transform.position = parent.transform.position - Vector3.up * minY;
            }
        }

        parent.transform.SetParent(container.transform, true);
    }

    //UTILS 
    //-------------

    private bool IsPointerOverUi()
    {
        // Check if EventSystem exists
        if (EventSystem.current == null) return false;

        // Create a pointer event for the current mouse position
        PointerEventData eventData = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        // Create a list to hold the results
        List<RaycastResult> results = new();

        // Manual Raycast against the UI
        EventSystem.current.RaycastAll(eventData, results);

        // If we hit at least one UI element, return true
        return results.Count > 0;
    }

    private void HandleTransformChange(TransSpace space, TransType type, Axis axis, float value)
    {

        if (!_selectionManager.SelectionExist) return;
        Transform t = _selectionManager.SelectionTransform;

        // Apply the change
        ApplyModification(t, space, type, axis, value);

        _gizmoManager.UpdateGizmoPosition(t);

        // Optional: Sync Physics if needed
        Physics.SyncTransforms();
    }

    private void ApplyModification(Transform t, TransSpace space, TransType type, Axis axis, float val)
    {
        // Helper to modify a single component of a vector
        static Vector3 ModifyVector(Vector3 original, Axis a, float v)
        {
            if (a == Axis.X) original.x = v;
            if (a == Axis.Y) original.y = v;
            if (a == Axis.Z) original.z = v;
            return original;
        }

        if (space == TransSpace.Local)
        {
            if (type == TransType.Position) t.localPosition = ModifyVector(t.localPosition, axis, val);
            if (type == TransType.Rotation) t.localEulerAngles = ModifyVector(t.localEulerAngles, axis, val);
            if (type == TransType.Scale) t.localScale = ModifyVector(t.localScale, axis, val);
        }
        else // Global
        {
            if (type == TransType.Position) t.position = ModifyVector(t.position, axis, val);
            if (type == TransType.Rotation) t.eulerAngles = ModifyVector(t.eulerAngles, axis, val);
            // Global Scale is usually read-only in Unity because of skewing, skip or implement carefully
        }
    }
}