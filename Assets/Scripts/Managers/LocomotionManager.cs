using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionManager : MonoBehaviour
{
    [Header("Locomotion Actions")]
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference teleportAction;
    [SerializeField] InputActionReference snapTurnAction;
    [SerializeField] InputActionReference continuousTurnAction;

    private bool _moveEnabled = true;
    private bool _teleportEnabled = true;
    private bool _snapTurnEnabled = true;
    private bool _continuousTurnEnabled = false;

    public void LockMove(bool locked)
    {
        _moveEnabled = !locked;
    }

    public void LockTeleport(bool locked)
    {
        _teleportEnabled = !locked;
    }

    public void LockSnapTurn(bool locked)
    {
        _snapTurnEnabled = !locked;
    }

    public void LockContinuousTurn(bool locked)
    {
        _continuousTurnEnabled = !locked;
    }

    // XRInteraction toolkit can autonomously enable the actions 
    // I need too check every frame if this happend and reToggle 
    private void Update()
    {
        ToggleAction(moveAction, _moveEnabled);
        ToggleAction(teleportAction, _teleportEnabled);
        ToggleAction(snapTurnAction, _snapTurnEnabled);
        ToggleAction(continuousTurnAction, _continuousTurnEnabled);
    }

    private void ToggleAction(InputActionReference actionRef, bool isEnabled)
    {
        if (actionRef?.action == null) return;

        if (isEnabled)
        {
            if (!actionRef.action.enabled) actionRef.action.Enable();
        }
        else
        {
            if (actionRef.action.enabled) actionRef.action.Disable();
        }
    }
}