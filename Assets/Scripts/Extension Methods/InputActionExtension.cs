using UnityEngine.InputSystem;

public static class InputActionsExtensions
{
    // For direct actions
    public static void SetState(this InputAction action, bool active)
    {
        if (action == null) return;
        if (active) action.Enable();
        else action.Disable();
    }

    // Add this for References (to avoid .action.ToInputAction() mess)
    public static void SetState(this InputActionReference actionRef, bool active)
    {
        if (actionRef != null && actionRef.action != null)
        {
            actionRef.action.SetState(active);
        }
    }
}