using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_SnapMenu : HM_Group
{
    [SerializeField] HM_StopFollow stopFollowCard;
    bool _stopFollowIn = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        HandleStopFollowCard(_deps.selection.Selected);
        _deps.selection.OnSelectionChanged += HandleStopFollowCard;
        HandMenuComunication.OnStopFollow += HideStopFollow;
    }

    /// <summary>
    /// Adds the card Stop follow when the currently selected objects 
    /// has the component SnapFollow
    /// </summary>
    /// <param name="interactable"></param>
    private void HandleStopFollowCard(XRGrabInteractable interactable)
    {
        if (_stopFollowIn == false &&
            interactable != null &&
            interactable.HasComponent<SnapFollow>())
        {
            _deps.handMenu.AddMenuEntries(new() { stopFollowCard }, _deps);
            _stopFollowIn = true;
        }
        else if (_stopFollowIn)
        {
            _deps.handMenu.RemoveMenuEntries(new() { stopFollowCard });
            _stopFollowIn = false;
        }
    }

    private void HideStopFollow() => HandleStopFollowCard(null);


    public override void OnRemove()
    {
        base.OnRemove();
        _deps.selection.OnSelectionChanged -= HandleStopFollowCard;
        HandMenuComunication.OnStopFollow -= HideStopFollow;
    }

}
