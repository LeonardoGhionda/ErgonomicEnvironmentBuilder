using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_LockAllPos : HM_Toggle
{
    public override void OnClick()
    {
        base.OnClick();

        foreach (var grabbable in GameObject.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
        {
            grabbable.trackPosition = !_state;
        }
    }
}
