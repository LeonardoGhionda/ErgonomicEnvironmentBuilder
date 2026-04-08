using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    // Names of the scene files used in the project. These should match the actual scene names in the Unity Editor.
    public enum SceneName
    {
        Main,
        Simulation,
    }

    private IAppState _currentState;
    private IAppState _lastState;

    private AppActions _appInput;


    public bool VRProfile { get; private set; }
    public bool DTProfile => !VRProfile;

    private GameObject VRPlayer => DependencyProvider.VRPlayer;
    private GameObject DTPlayer => DependencyProvider.DTPlayer;


    [Header("Views")]
    [SerializeField] private MenuRoomView menuRoomView;
    [SerializeField] private ImmersiveEditorView iEditorView;
    [SerializeField] private RoomTestView roomTestView;
    [SerializeField] private OptionView optionView;
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private NewRoomUI newRoomUI;
    [SerializeField] private EditorHUDView editorHUD;

    // Desktop States
    public MainMenuState MainMenu { get; private set; }
    public NewRoomState NewRoom { get; private set; }
    public LoadRoomState LoadRoom { get; private set; }
    public OptionState Option { get; private set; }
    public PauseMenuState Pause { get; private set; }
    public RoomEditorState RoomEditor { get; private set; }
    public SpectatorState Spectator { get; private set; }

    // VR States
    public MenuRoomState MenuRoom { get; private set; }
    public ImmersiveEditor ImmersiveEditor { get; private set; }
    public RoomTestState TestRoom { get; private set; }

    private void Awake()
    {
        // Set up input
        // VR basic input is handled by the XR Interaction Toolkit Input action asset
        _appInput = new AppActions();
        DependencyProvider.Input = _appInput;

        // Determine the profile based on a flag,
        // toggle automatically by the profile switch in the unity editor
#if USE_XR
        VRProfile = true;
#else
        VRProfile = false;
#endif
    }

    private void Start()
    {
        // Initialize the appropriate states based on the profile
        if (VRProfile)
        {
            InitVR();
        }
        else
        {
            InitDT();
        }

        // Call the Enter function of the initial state
        _currentState.Enter();
    }

    private void InitVR()
    {
        // Ensure VRPlayer is active and DTPlayer is inactive at the start
        VRPlayer.SetActive(true);
        Destroy(DTPlayer);

        // Initialize VR states with the necessary dependencies
        MenuRoom = new(this, _appInput, menuRoomView);
        ImmersiveEditor = new(this, _appInput, iEditorView);
        TestRoom = new(this, _appInput, roomTestView);

        // Start in the Menu Room state
        _currentState = MenuRoom;
    }

    private void InitDT()
    {
        // Ensure DTPlayer is active and VRPlayer is inactive at the start
        DTPlayer.SetActive(true);
        Destroy(VRPlayer);

        // Initialize desktop states with the necessary dependencies
        MainMenu = new(this, _appInput, mainMenuUI);
        NewRoom = new(this, _appInput, newRoomUI);
        LoadRoom = new(this, _appInput);
        Option = new(this, _appInput, optionView);
        Pause = new(this, _appInput);
        RoomEditor = new(this, _appInput, editorHUD);
        Spectator = new(this, _appInput);

        // Start in the Main Menu state
        _currentState = MainMenu;
    }

    private void Update()
    {
        // Delegate the Update call to the current state
        _currentState.UpdateState();
    }

    /// <summary>
    /// Transitions the application to a new state by exiting the current state and entering the specified state.
    /// </summary>
    /// <remarks>If the specified state is null, the application will terminate immediately. Otherwise, the
    /// current state is exited before entering the new state. This method should be called to manage state transitions
    /// within the application's lifecycle.</remarks>
    /// <param name="newState">The new application state to transition to. If null, the application will quit.</param>
    internal void ChangeState(IAppState newState)
    {
        _lastState = _currentState;

        _currentState?.Exit();
        _currentState = newState;

        if (_currentState == null)
        {
            ExitApplication();
            return;
        }

        _currentState?.Enter();
    }

    internal void RevertToLastState()
    {
        ChangeState(_lastState);
    }

    /// <summary>
    /// Transitions the application to a new state and loads the specified scene.
    /// </summary>
    /// <remarks>If the new scene is not the main scene, the method subscribes to the scene loaded event to
    /// perform additional initialization after the scene is loaded. When transitioning to the main scene, any existing
    /// state is exited and cleared.</remarks>
    /// <param name="newState">The application state to activate after the new scene is loaded. This parameter is ignored if the new scene is
    /// the main scene.</param>
    /// <param name="newScene">The scene to load. If set to the main scene, the current state is cleared and the scene is loaded in single
    /// mode; otherwise, the new state is set and the scene is loaded additively.</param>
    internal void ChangeStateInNewScene(IAppState newState, SceneName newScene)
    {
        _lastState = _currentState;

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