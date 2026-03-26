using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    public enum SceneName
    {
        Main,
        Simulation,
    }

    private IAppState _currentState;

    public AppActions AppInput => _appInput;
    AppActions _appInput;

    [Header("Views")]
    [SerializeField] private MenuRoomView menuRoomView;
    [SerializeField] private ImmersiveEditorView iEditorView;
    [SerializeField] private RoomTestView roomTestView;
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private NewRoomUI newRoomUI;
    [SerializeField] private EditorHUDView editorHUD;

    [Header("Room Containers")]
    [SerializeField] private GameObject menuRoomContainer;
    [SerializeField] private GameObject viewContainer;

    [Header("Managers")]
    [SerializeField] private RoomBuilderManager roomBuilderManager;
    [SerializeField] private GizmoManager gizmoManager;
    [SerializeField] private DTSelectionManager DTSelectionManager;
    [SerializeField] private VRSelectionManager VRSelectionManager;
    [SerializeField] private MeasureManager measureManager;
    [SerializeField] private HandMenuManager handMenuManager;
    [SerializeField] private ScaleManager scaleManager;

    [SerializeField] private NetworkManager networkManager;

    [Header("Components")]
    [SerializeField] private CameraController cameraController;

    [Header("Players")]
    [SerializeField] private GameObject VRPlayer;
    [SerializeField] private GameObject DTPlayer;

    public CameraController CameraController => cameraController;

    public MainMenuState MainMenu { get; private set; }
    public NewRoomState NewRoom { get; private set; }
    public LoadRoomState LoadRoom { get; private set; }
    public OptionState Option { get; private set; }
    public PauseMenuState Pause { get; private set; }
    public RoomEditorState RoomEditor { get; private set; }
    public SpectatorState Spectator { get; private set; }

    public MenuRoomState MenuRoom { get; private set; }
    public ImmersiveEditor ImmersiveEditor { get; private set; }
    public RoomTestState TestRoom { get; private set; }

    private void Awake()
    {
        _appInput = new AppActions();
    }

    private void Start()
    {
#if USE_XR
        VRPlayer.SetActive(true);
        Destroy(DTPlayer);

        MenuRoom = new(this, AppInput, menuRoomView);
        ImmersiveEditor = new(this, AppInput, roomBuilderManager, VRPlayer, iEditorView, VRSelectionManager, measureManager, handMenuManager);
        TestRoom = new(this, AppInput, roomTestView);

        _currentState = MenuRoom;
#else
        DTPlayer.SetActive(true);
        Destroy(VRPlayer);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        MainMenu =   new(this, AppInput, mainMenuUI, DTPlayer);
        NewRoom =    new(this, AppInput, newRoomUI, roomBuilderManager);
        LoadRoom =   new(this, AppInput, roomBuilderManager);
        Option =     new(this, AppInput);
        Pause =      new(this, AppInput);
        RoomEditor = new(this, AppInput, editorHUD, roomBuilderManager, gizmoManager, DTSelectionManager, measureManager);
        Spectator =  new(this, AppInput);

        _currentState = MainMenu;
#endif
        _currentState.Enter();
    }

    private void Update()
    {
        _currentState.UpdateState();
    }

    internal void ChangeState(IAppState newState)
    {
        _currentState?.Exit();
        _currentState = newState;

        if (_currentState == null)
        {
            Application.Quit();
            return;
        }

        _currentState?.Enter();
    }

    internal void ChangeStateInNewScene(IAppState newState, SceneName newScene)
    {
        _currentState?.Exit();

        bool newSceneIsMain = newScene == SceneName.Main;

        if (newSceneIsMain)
        {
            _currentState = null;
            SceneManager.LoadScene(newScene.ToString(), LoadSceneMode.Single);
        }
        else
        {
            _currentState = newState;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(newScene.ToString(), LoadSceneMode.Additive);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _currentState?.Enter();
    }

    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}