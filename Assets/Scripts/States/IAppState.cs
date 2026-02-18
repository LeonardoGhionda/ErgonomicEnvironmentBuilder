public interface IAppState
{
    // Called when state start (Setup input, UI, Camera)
    abstract public void Enter();

    // Called every frame
    abstract public void UpdateState();

    //Called when state is over (Cleanup input, nascondi UI)
    abstract public void Exit();
}
