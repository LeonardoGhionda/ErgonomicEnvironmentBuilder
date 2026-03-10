using UnityEngine;

public class SpectatorState : AbsAppState
{
    public SpectatorState(
        StateManager manager, 
        AppActions input
        ) : base(manager, input)
    {
    }

    public override void Enter()
    {
        string json = GameObject.FindAnyObjectByType<RoomBuilderManager>().RoomJson;
        RoomManagementTools.BuildSpectatorRoom(json);
    }

    public override void Exit()
    {
    }

    public override void UpdateState()
    {
    }
}