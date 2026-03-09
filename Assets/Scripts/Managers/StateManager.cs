using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    public enum SceneName
    {
        Main,
        Simulation,
    }

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
    [SerializeField] private DTSelectionManager DTSelectionManager;
    [SerializeField] private VRSelectionManager VRSelectionManager;
    [SerializeField] private MeasureManager measureManager;
    [SerializeField] private HandMenuManager handMenuManager;
    [SerializeField] private ScaleManager scaleManager;

    [Header("Components")]
    [SerializeField] private CameraController cameraController;

    [Header("Players")]
    [SerializeField] private GameObject VRPlayer;
    [SerializeField] private GameObject DTPlayer;

    // Public Getter
    public CameraController CameraController => cameraController;

    // --- DT STATES ---
    public MainMenuState MainMenu { get; private set; }
    public NewRoomState NewRoom { get; private set; }
    public LoadRoomState LoadRoom { get; private set; }
    public OptionState Option { get; private set; }
    public PauseMenuState Pause { get; private set; }
    public RoomEditorState RoomEditor { get; private set; }

    // --- VR STATES ---
    public MenuRoomState MenuRoom { get; private set; }
    public ImmersiveEditor ImmersiveEditor { get; private set; }
    public RoomTestState TestRoom { get; private set; }

    private void Awake()
    {
        // Initialize Input
        _appInput = new AppActions();

        DontDestroyOnLoad(transform.parent.gameObject); // Make all managers persistent through different scenes
        DontDestroyOnLoad(VRPlayer);
    }

    private void Start()
    {

        // Initialize states and set the first state to enter

#if USE_XR
        VRPlayer.SetActive(true);

        //---STATE SETUP---
        MenuRoom = new(this, AppInput, menuRoomContainer, menuRoomView, roomBuilderManager, VRPlayer);
        ImmersiveEditor = new(this, AppInput, roomBuilderManager, VRPlayer, iEditorView, VRSelectionManager, measureManager, handMenuManager, scaleManager);
        TestRoom = new(this, AppInput, roomBuilderManager, VRPlayer);

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
        RoomEditor = new(this, AppInput, editorHUD, roomBuilderManager, gizmoManager, DTSelectionManager, measureManager);
        

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
        if (currentState == null)
        {
            Destroy(this); //close the app
            return;
        }
        currentState?.Enter();
    }

    internal void ChangeStateInNewScene(IAppState newState, SceneName newScene)
    {
        currentState?.Exit();
        currentState = newState;

        if (currentState == null)
        {
            Destroy(this); //close the app
            return;
        }

        // Subscribe to the scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(newScene.ToString());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode LSM)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        currentState?.Enter();
    }

    public void ExitApplication()
    {
        ChangeState(null);
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
