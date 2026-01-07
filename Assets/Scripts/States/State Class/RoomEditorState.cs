using Dummiesman;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomEditorState : AbsAppState
{
    private EditorHUDView _hud;

    //managers (get from State manager)
    private FreeCameraController _camController;
    private RoomBuilderManager _rbm;
    private GizmoManager _gizmoManager;
    private SelectionManager _selectionManager;

    private Vector2 mousePos => _input.RoomEditCommon.Pointer.ReadValue<Vector2>();

    private bool _uiMode = false;

    public RoomEditorState(
        StateManager manager,
        AppActions input,
        EditorHUDView editorHUD,
        RoomBuilderManager roomBuilderManager,
        GizmoManager gizmoManager,
        SelectionManager selectionManager) : base(manager, input)
    {
        _hud = editorHUD;
        _camController = manager.cameraController;
        _rbm = roomBuilderManager;
        _gizmoManager = gizmoManager;
        _selectionManager = selectionManager;
    }

    public override void Enter()
    {
        // Setup UI
        _hud.ShowSelectionMenu(null, null);
        _hud.ToggleExitMenu(false);
        _hud.ToggleModelsMenu(false);
        _hud.gameObject.SetActive(true);

        // UI events
        _hud.OnSaveClicked += SaveRoom;
        _hud.OnQuitClicked += QuitRoom;
        _hud.OnModelButtonClicked += PlaceModel;
        _hud.OnTranformButtonClicked += (TransformMode mode) => ChangeTransformType(mode);

        // Input events
        _input.RoomEditCommon.Enable();
        _input.RoomEditOrtho.Enable();
        
        _input.RoomEditCommon.PauseMenu.performed += OnTogglePauseMenu;
        _input.RoomEditCommon.ModelsMenu.performed += OnToggleModels;
        _input.RoomEditCommon.Select.performed += OnSelectActionPerformed;
        _input.RoomEditCommon.Select.canceled += OnSelectActionCanceled;
        _input.RoomEditCommon.SwitchView.performed += OnSwitchView;
        _input.RoomEditCommon.Delete.performed += (ctx) => _selectionManager.DeleteSelected();


        _input.Ui.EnableCamera.performed += (ctx) => SetUIMode(false);
        _input.Ui.EnableCamera.canceled += (ctx) => SetUIMode(true);
        _input.Ui.Cancel.performed += OnCloseMenu;


        // Start Camera
        _camController.enabled = true;
        _camController.Init(_input);

        // Ui Action always enabled
        _input.Ui.Enable();

        // Start in Edit Mode
        SetUIMode(false);

        //start managers
        _gizmoManager.Init(_camController.Camera, _camController);
        _selectionManager.Init(_camController.Camera);
    }

    public override void Exit()
    {
        // Cleanup Events
        _hud.OnSaveClicked -= SaveRoom;
        _hud.OnQuitClicked -= QuitRoom;
        _hud.OnModelButtonClicked -= PlaceModel;
        _hud.OnTranformButtonClicked += (TransformMode mode) => ChangeTransformType(mode);

        _input.RoomEditCommon.PauseMenu.performed -= OnTogglePauseMenu;
        _input.RoomEditCommon.ModelsMenu.performed -= OnToggleModels;
        _input.RoomEditCommon.Select.performed -= OnSelectActionPerformed;
        _input.RoomEditCommon.Select.canceled -= OnSelectActionCanceled;
        _input.RoomEditCommon.SwitchView.performed -= OnSwitchView;
        _input.RoomEditCommon.Delete.performed -= (ctx) => _selectionManager.DeleteSelected();

        _input.Ui.EnableCamera.performed -= (ctx) => SetUIMode(false);
        _input.Ui.EnableCamera.canceled -= (ctx) => SetUIMode(true);
        _input.Ui.Cancel.performed -= OnCloseMenu;


        // Cleanup Maps
        _input.RoomEditCommon.Disable();
        _input.RoomEditOrtho.Disable();
        _input.RoomEditPerspective.Disable();
        _input.Ui.Disable();

        //turn off ui
        _hud.gameObject.SetActive(false);

        //gizmpo manager
        _gizmoManager.Stop();
    }

    public override void UpdateState()
    {
        if (_selectionManager.SelectionExist)
            _gizmoManager.HandleDragging(_selectionManager.SelectionTransform, mousePos);
        
        _gizmoManager.ScaleHandlesByCameraDistance();
    }

    // --- INPUT HANDLERS ---

    private void OnSelectActionPerformed(InputAction.CallbackContext ctx)
    {
        //if there is a selection we have to check if the user wants to use an handle first
        if (_selectionManager.SelectionExist)
        { 
            if (_gizmoManager.TrySelectHandle(mousePos))
            {
                return;
            }
        }

        // No handle selected, proceed with normal selection
        _selectionManager.Select();

        // Open/close panel
        if (_selectionManager.SelectionExist)
        {
            _hud.ShowSelectionMenu(_selectionManager.SelectionGO, _gizmoManager);
            _gizmoManager.SetGizmoActive(true, _selectionManager.SelectionTransform);
        }
        else
        {
            _hud.HideAllMenus();
            _gizmoManager.RemoveGizmo();
        }

    }

    private void OnSelectActionCanceled(InputAction.CallbackContext context)
    {
        if (_selectionManager.SelectionExist) 
            _gizmoManager.DeselectHandle(_selectionManager.SelectionTransform);
        _camController.MenuMode(false);
    }


    private void OnSwitchView(InputAction.CallbackContext ctx)
    {
        _camController.ToggleView();
    }

    private void OnTogglePauseMenu(InputAction.CallbackContext ctx)
    {

    }

    private void OnToggleModels(InputAction.CallbackContext ctx)
    {
        _hud.ToggleModelsMenu(true);
        SetUIMode(true);
    }

    private void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        _hud.HideAllMenus();
        SetUIMode(false);
    }

    /// <summary>
    /// Lock/Unock camera and Enable/Disable input
    /// </summary>
    /// <param name="value"></param>
    private void SetUIMode(bool value)
    {
        _uiMode = value;
        if (value) // UI
        {
            // --- input ---
            _input.RoomEditCommon.Disable();
            _input.RoomEditOrtho.Disable();
            _input.RoomEditPerspective.Disable();

            // --- camera ---
            _camController.enabled = false;
        }
        else // CAMERA
        {
            // --- input ---
            _input.RoomEditCommon.Enable();
            if (_camController.IsOrtho) _input.RoomEditOrtho.Enable();
            else _input.RoomEditPerspective.Enable();

            // --- camera ---
            _camController.enabled = true;
        }
    }

    // --- ACTIONS RESPONCE ---

    private void SaveRoom()
    {
        RoomDataExporter.Save(_rbm.RoomName);
        Debug.Log("Room Saved!");
    }

    private void QuitRoom()
    {
        SaveRoom();
        Debug.LogWarning("TODO: Clear Scene");
        _manager.ChangeState(_manager.MainMenu);
    }

    private void ChangeTransformType(TransformMode mode)
    {
        _gizmoManager.SetMode(mode, _selectionManager.SelectionTransform);
    }

    private void PlaceModel(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("OBJ path is null or empty");
            return;
        }
        OBJLoader loader = new();
        GameObject obj = loader.Load(path);
        SetUpModel(obj, path, GameObject.Find("Objects Container"));

        _hud.HideAllMenus();

        
        _selectionManager.ChangeSelectedObject(obj.GetComponentInChildren<InteractableObject>());

        _hud.ShowSelectionMenu(_selectionManager.SelectionGO, _gizmoManager);
    }

    public static void SetUpModel(GameObject parent, string path, GameObject container)
    {
        parent.name = $"[P] {parent.name}";

        parent.AddComponent<InteractableParent>().Path = path;

        parent.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5f;

        MeshRenderer[] childrenMRs = parent.GetComponentsInChildren<MeshRenderer>();

        float minY = 0.0f;

        foreach (var mr in childrenMRs)
        {
            var bc = mr.gameObject.AddComponent<BoxCollider>();
            var bottom = (bc.center.y - bc.size.y / 2f);
            if (bottom < minY) minY = bottom;
            mr.gameObject.AddComponent<InteractableObject>();
        }

        if (Camera.main.GetComponent<FreeCameraController>().IsOrtho)
        {
            parent.transform.position = Vector3.Scale(parent.transform.position, new Vector3(1f, 0f, 1f));
            parent.transform.position = parent.transform.position - Vector3.up * minY;
        }

        parent.transform.SetParent(container.transform, true);
    }
}