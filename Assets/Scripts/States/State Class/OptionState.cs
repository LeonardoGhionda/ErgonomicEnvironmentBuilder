using System;
using UnityEngine.InputSystem;

public class OptionState : AbsAppState
{
    private OptionView _view;

    public OptionState(StateManager manager, AppActions input, OptionView view) : base(manager, input)
    {
        _view = view;
    }

    public override void Enter()
    {
        _input.Ui.Enable();
        _input.Ui.GoBack.performed += OnGoBackPerformed;

        _view.gameObject.SetActive(true);
    }

    public override void Exit()
    {
        _input.Ui.GoBack.performed -= OnGoBackPerformed;
        _input.Ui.Disable();

        _view.gameObject.SetActive(false);
    }

    public override void UpdateState()
    {
    }

    private void OnGoBackPerformed(InputAction.CallbackContext _)
    {
        _manager.RevertToLastState();
    }
}