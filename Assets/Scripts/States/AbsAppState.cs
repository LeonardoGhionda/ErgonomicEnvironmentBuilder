/// <summary>
/// This class was created to have manager and input in all the state classes
/// </summary>
public abstract class AbsAppState : IAppState
{
    protected StateManager _manager;
    protected AppActions _input;

    //constructor
    protected AbsAppState(StateManager manager, AppActions input)
    {
        _manager = manager;
        _input = input;
    }

    abstract public void Enter();
    abstract public void UpdateState();
    abstract public void Exit();

}
