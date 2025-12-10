using System;

public class MainMenuState : AbsAppState
{
    private MainMenuUI _view;

    // Costruttore: riceve View e Manager
    public MainMenuState(StateManager manager, AppActions input, MainMenuUI view) : base(manager, input)
    {
        _view = view;
    }

    override public void Enter()
    {
        _input.Ui.Enable();
        _view.Show();

        //event subscription
        _view.OnNewRoomClicked += GoNewRoom;
        _view.OnLoadRoomClicked += GoLoadRoom;
        _view.OnOptionsClicked += GoOption;
    }

    override public void Exit()
    {
        //event unsubription
        _view.OnNewRoomClicked -= GoNewRoom;
        _view.OnLoadRoomClicked -= GoLoadRoom;
        _view.OnOptionsClicked -= GoOption;

        _input.Ui.Disable();
        _view.Hide();
    }

    override public void UpdateState() { }

    private void GoNewRoom()
    {
        _manager.ChangeState(_manager.NewRoom);
    }

    private void GoLoadRoom()
    {
        _manager.ChangeState(_manager.LoadRoom);
    }

    private void GoOption()
    {
        throw new NotImplementedException();
    }
}