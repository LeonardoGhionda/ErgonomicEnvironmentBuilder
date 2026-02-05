using System;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_Snap : HM_Base
{
    XRGrabInteractable _snap1;
    Renderer s1Renderer => _snap1.GetComponent<MeshRenderer>();
    SnapTools _sTool;
    [SerializeField] FollowCameraUI _tutorialText;
    [SerializeField] Material _snapSelectedMaterial;
    Material _originalMaterial;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sTool = new SnapTools();
    }

    public override void OnClick()
    {
        base.OnClick();

        // Safety Check: Is something actually selected?
        if (_deps.selection.Selected == null) return;

        _snap1 = _deps.selection.Selected;

        // Clear strictly AFTER saving the reference
        _deps.selection.ClearSelection();

        // Save and Change Material
        _originalMaterial = s1Renderer.material;
        s1Renderer.material = _snapSelectedMaterial;

        // UI Setup
        _tutorialText.gameObject.SetActive(true);
        _tutorialText.transform.SetParent(_snap1.transform);
        _tutorialText.transform.localPosition = Vector3.zero;

        // Subscribe
        _deps.selection.OnSelectionChangedNotNull += ExecuteSnap;

        // Lock UI
        _deps.handMenu.Show(false);
        _deps.handMenu.Lock = true;
    }

    // Usually called if the user switches tools or closes the menu manually
    public override void OnRemove()
    {
        base.OnRemove();
        ResetState();
    }

    private void ExecuteSnap(XRGrabInteractable interactable)
    {
        // Stop listening immediately!
        _deps.selection.OnSelectionChangedNotNull -= ExecuteSnap;

        // Handle Cancellation (User deselected or cleared)
        if (interactable == null)
        {
            ResetState();
            return;
        }

        // Perform Logic
        XRGrabInteractable snap2 = interactable;

        _deps.selection.ClearSelection();

       float maxSnapDistance = _snap1.GetComponent<BoxCollider>().size.MaxComponent() + snap2.GetComponent<BoxCollider>().size.MaxComponent() + 6f;

        bool success = _sTool.SnapToTarget(_snap1.transform, snap2.transform, maxSnapDistance);

        // Update or add the target to follow
        if (_snap1.TryGetComponent<SnapFollow>(out var snapFollow)) snapFollow.Init(snap2.transform);
        else _snap1.AddComponent<SnapFollow>().Init(snap2.transform);

        // Inform of the snap success
        HandMenuComunication.OnSnapPerformed?.Invoke();
        _deps.handMenu.Show(false);

        // Clean up and Unlock
        ResetState();
    }

    private void ResetState()
    {
        // Hide Text
        if (_tutorialText != null)
        {
            _tutorialText.transform.SetParent(null); // Reset parent so it doesn't get deleted with obj
            _tutorialText.gameObject.SetActive(false);
        }

        if (_snap1 != null)
        {
            // Material Reset
            s1Renderer.material = _originalMaterial;
        }

        // Unlock Hand
        _deps.handMenu.Lock = false;

        // Ensure we don't have lingering listeners (safe to call even if not subscribed)
        _deps.selection.OnSelectionChangedNotNull -= ExecuteSnap;
    }
}
