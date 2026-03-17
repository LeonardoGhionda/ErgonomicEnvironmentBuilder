using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RoomTestState : AbsAppState
{
    private readonly RoomBuilderManager _rbm;
    private readonly GameObject _vrPlayer;
    private readonly VRHostBroadcaster _inviteBroadcaster;
    private readonly NetworkPrefabMimic _networkPrefabMimic;

    public RoomTestState(
        StateManager manager,
        AppActions input,
        RoomBuilderManager rbm,
        GameObject vrPlayer
    ) : base(manager, input)
    {
        _rbm = rbm;
        _vrPlayer = vrPlayer;
        _inviteBroadcaster = GameObject.FindAnyObjectByType<VRHostBroadcaster>(FindObjectsInactive.Include);

        _networkPrefabMimic = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
            .Select(p => p.Prefab.GetComponent<NetworkPrefabMimic>())
            .First(mimic => mimic != null);

    }

    public override void Enter()
    {
        _inviteBroadcaster.enabled = true;
        _inviteBroadcaster.StartHostingAndBroadcasting(_rbm.RoomName);

        RoomManagementTools.CreateTestRoom(_rbm.RoomName, _networkPrefabMimic.gameObject);

        Physics.SyncTransforms();

        Vector3 insideWallPosition = RoomManagementTools.FindInternalPoint();
        insideWallPosition.y = 0;
        _vrPlayer.transform.position = insideWallPosition;

        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = false;
        
        _vrPlayer.GetComponent<XROriginMoCapSync>().enabled = true;
    }

    public override void Exit()
    {
        NearFarInteractor[] interactors = GameObject.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (NearFarInteractor item in interactors) item.enableFarCasting = true;

        _vrPlayer.GetComponent<XROriginMoCapSync>().enabled = false;
    }

    public override void UpdateState() {}
}