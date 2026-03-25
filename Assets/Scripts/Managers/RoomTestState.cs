using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RoomTestState : AbsAppState
{
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;
    private readonly InvitationBroadcaster _inviteBroadcaster;
    private readonly NetworkPrefabMimic _networkPrefabMimic;
    private readonly RoomTestView _view;

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
    }

    public override void Enter()
    {
        _inviteBroadcaster.enabled = true;
        _inviteBroadcaster.StartBroadcasting(_rbm.RoomName);

        RoomManagementTools.CreateTestRoom(_rbm.RoomName, _networkPrefabMimic.gameObject);

        Physics.SyncTransforms();

        Vector3 insideWallPosition = RoomManagementTools.FindInternalPoint();
        insideWallPosition.y = 0;
        _vrPlayer.transform.position = insideWallPosition;

        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = false;
        
        _vrPlayer.GetComponent<XROriginMoCapSync>().enabled = true;
        _view.gameObject.SetActive(true);
        _view.Init();
    }

    public override void Exit()
    {
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = true;

        _vrPlayer.GetComponent<XROriginMoCapSync>().enabled = false;

        _view.gameObject.SetActive(false);
    }

    public override void UpdateState() {}
}