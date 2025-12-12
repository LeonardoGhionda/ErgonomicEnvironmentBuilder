using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateManager : MonoBehaviour
{

    // Singleton Instance
    public static StateManager Instance { get; private set; }
    IAppState currentState;

    public AppActions AppInput => _appInput;
    AppActions _appInput;

    [Header("UI Pages")]
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private NewRoomUI newRoomUI;
    [SerializeField] private EditorHUDView editorHUD;

    [Header("Managers")]
    [SerializeField] private RoomBuilderManager roomBuilderManager;
    [SerializeField] private GizmoManager gizmoManager;
    
    [Header("Components")]
    [SerializeField] public FreeCameraController cameraController;


    //---STATES---
    public MainMenuState MainMenu { get; private set; }
    public NewRoomState NewRoom {  get; private set; }
    public LoadRoomState LoadRoom { get; private set; }
    public OptionState Option { get; private set; }
    public PauseMenuState Pause { get; private set; }
    public RoomEditorState RoomEditor { get; private set; }

    private void Awake()
    {
        // Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize Input
        _appInput = new AppActions();
    }

    private void Start()
    {
        //---STATES SETUP---
        MainMenu =   new(this, AppInput, mainMenuUI);
        NewRoom =    new(this, AppInput, newRoomUI, roomBuilderManager);
        LoadRoom =   new(this, AppInput);
        Option =     new(this, AppInput);
        Pause =      new(this, AppInput);
        RoomEditor = new(this, AppInput, editorHUD, roomBuilderManager, gizmoManager);

        //first state iniziaization
        currentState = MainMenu;
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
        currentState?.Enter();
    }
}
