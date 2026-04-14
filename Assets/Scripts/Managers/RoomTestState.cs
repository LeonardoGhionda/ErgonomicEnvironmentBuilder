using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RoomTestState : AbsAppState
{
    private readonly GameObject _vrPlayer;
    private readonly InvitationBroadcaster _inviteBroadcaster;
    private readonly NetworkPrefabMimic _networkPrefabMimic;
    private readonly RoomTestView _view;

    private XROriginMoCapSync _mocapSync;

    private readonly RoomBuilderManager _rbm;
    private readonly HandMenuManager _handMenuManager;

    private bool _hmWaitRelease = false;


    public RoomTestState(
        StateManager manager,
        AppActions input,
        RoomTestView view
    ) : base(manager, input)
    {
        _rbm = Managers.Get<RoomBuilderManager>();
        _vrPlayer = DependencyProvider.VRPlayer;
        _inviteBroadcaster = GameObject.FindAnyObjectByType<InvitationBroadcaster>(FindObjectsInactive.Include);

        _networkPrefabMimic = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
            .Select(p => p.Prefab.GetComponent<NetworkPrefabMimic>())
            .First(mimic => mimic != null);

        _view = view;
        _handMenuManager = Managers.Get<HandMenuManager>();
    }

    public override void Enter()
    {
        // Start broadcasting invitation
        _inviteBroadcaster.enabled = true;
        _inviteBroadcaster.StartBroadcasting(_rbm.RoomName);

        //  Create Room
        RoomManagementTools.CreateTestRoom(_rbm.RoomName, _networkPrefabMimic.gameObject);
        Physics.SyncTransforms();

        // Disable far casting (during test only real-life interaction are allowed)
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = false;

        // Start mocap sync with HMD
        _mocapSync = _vrPlayer.GetComponent<XROriginMoCapSync>();
        _mocapSync.enabled = true;

        // Move player inside the wall 
        Vector3 insideWallPosition = RoomManagementTools.FindInternalPoint();
        insideWallPosition.y = 0;
        _mocapSync.SetPosition(insideWallPosition);
        _mocapSync.CalibrateRotation();

        _handMenuManager.Init();

        // View
        _view.gameObject.SetActive(true);
        _view.Init();


        // Input
        _input.HandMenu.Enable();
        _input.HandMenu.MoveEntries.started += MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled += MoveHandMenuEntriesReleased;
        _input.HandMenu.Confirm.performed += HandMenuConfirm;
        _input.HandMenu.Open.performed += MenuButtonClicked;

        _input.VRMenu.ToggleScreen.Enable();

        Managers.Get<ScreenShareManager>().gameObject.SetActive(true);


        Managers.Get<WalkDistanceManager>().StartCoroutine(InitAfterDelay());


        // Lock motion
        var locManager = Managers.Get<LocomotionManager>();
        locManager.LockMove(true);
        locManager.LockSnapTurn(true);

    }

    public override void Exit()
    {
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = true;

        _mocapSync.enabled = false;

        _view.gameObject.SetActive(false);

        // Input
        _input.HandMenu.Disable();
        _input.HandMenu.MoveEntries.started -= MoveHandMenuEntries;
        _input.HandMenu.MoveEntries.canceled -= MoveHandMenuEntriesReleased;
        _input.HandMenu.Confirm.performed -= HandMenuConfirm;
        _input.HandMenu.Open.performed -= MenuButtonClicked;

        _input.VRMenu.ToggleScreen.Disable();

        Managers.Get<ScreenShareManager>().gameObject.SetActive(false);

        // Unlock motion
        var locManager = Managers.Get<LocomotionManager>();
        locManager.LockMove(false);
        locManager.LockSnapTurn(false);

        // Close network
        _mocapSync.enabled = false;

        //Clear Room
        RoomManagementTools.CleanupRoom();

        _handMenuManager.TurnOff();

        Scene simulationScene = SceneManager.GetSceneByName(StateManager.SceneName.Simulation.ToString());
        if (simulationScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(simulationScene);
        }

        Managers.Get<WalkDistanceManager>().Stop();
    }

    public override void UpdateState() {}

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
    private System.Collections.IEnumerator InitAfterDelay()
    {
        // Wait for 4 seconds
        yield return new WaitForSeconds(2f);

        // Execute the initialization
        Managers.Get<WalkDistanceManager>().Init();
    }

}
