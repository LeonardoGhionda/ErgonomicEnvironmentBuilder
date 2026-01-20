using UnityEngine;

public class StateManager : MonoBehaviour
{
    IAppState currentState;

    public AppActions AppInput => _appInput;
    AppActions _appInput;

    [Header("Views")]
    // VR Views
    [SerializeField] private MenuRoomView menuRoomView;
    [SerializeField] private ImmersiveEditorView iEditorView;
    // DT Views
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private NewRoomUI newRoomUI;
    [SerializeField] private EditorHUDView editorHUD;

    [Header("Room Containers")]
    [SerializeField] private GameObject menuRoomContainer;

    [Header("Managers")]
    [SerializeField] private RoomBuilderManager roomBuilderManager;
    [SerializeField] private GizmoManager gizmoManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MeasureManager measureManager;

    [Header("Components")]
    [SerializeField] private CameraController cameraController;

    [Header("Players")]
    [SerializeField] private GameObject VRPlayer;
    [SerializeField] private GameObject DTPlayer;

    // Public Getter
    public CameraController CameraController => cameraController;

    // --- DT STATES ---
    public MainMenuState MainMenu { get; private set; }
    public NewRoomState NewRoom {  get; private set; }
    public LoadRoomState LoadRoom { get; private set; }
    public OptionState Option { get; private set; }
    public PauseMenuState Pause { get; private set; }
    public RoomEditorState RoomEditor { get; private set; }

    // --- VR STATES ---
    public MenuRoomState MenuRoom { get; private set; }
    public ImmersiveEditor ImmersiveEditor { get; private set; }

    private void Awake()
    {
        // Initialize Input
        _appInput = new AppActions();
    }

    private void Start()
    {
#if USE_XR
        VRPlayer.SetActive(true);
        MenuRoom = new(this, AppInput, menuRoomContainer, menuRoomView, roomBuilderManager);
        ImmersiveEditor = new(this, AppInput, roomBuilderManager, VRPlayer, iEditorView);

        currentState = MenuRoom;
#else
        DTPlayer.SetActive(true);
        //set cursor visible at start
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        //---STATES SETUP---
        MainMenu =   new(this, AppInput, mainMenuUI);
        NewRoom =    new(this, AppInput, newRoomUI, roomBuilderManager);
        LoadRoom =   new(this, AppInput, roomBuilderManager);
        Option =     new(this, AppInput);
        Pause =      new(this, AppInput);
        RoomEditor = new(this, AppInput, editorHUD, roomBuilderManager, gizmoManager, selectionManager, measureManager);
        

        //first state iniziaization
        currentState = MainMenu;
#endif
        currentState.Enter();
    }

    private void Update()
    {
        currentState.UpdateState();
    }

    internal void ChangeState(IAppState newState)
    {
        currentState?.Exit();
        currentState = newState;
        if (newState == null) Destroy(this); //close the app
        currentState?.Enter();
    }

    public void GoToMainMenu()
    {
        IAppState mainMenuState;
#if USE_XR
        VRPlayer.transform.position = Vector3.back * 7f;
        VRPlayer.transform.rotation = Quaternion.identity;
        mainMenuState = MenuRoom;

#else
        mainMenuState = MainMenu;
#endif
        ChangeState(mainMenuState);
    }


    private void OnDestroy()
    {
#if UNITY_EDITOR
        // Stop the Editor play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Close the built application
        Application.Quit();
#endif
    }
}
