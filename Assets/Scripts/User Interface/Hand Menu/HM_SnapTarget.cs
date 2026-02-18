using System;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_SnapTarget : HM_Base
{
    XRGrabInteractable _snap1;
    Renderer s1Renderer => _snap1.GetComponent<MeshRenderer>();
    SnapTools _sTool;
    [SerializeField] FollowCameraUI _tutorialText;
    [SerializeField] Material _snapSelectedMaterial;
    [SerializeField, Range(0.01f, 16f)] float textScaleFactor = 0.1f;
    [SerializeField] float minFont, maxFont;
    Material _originalMaterial;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_snap1 != null)
            SetupTutorialTXT();
    }
#endif


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

        // Tutorial Text Setup
        SetupTutorialTXT();

        // Subscribe
        // This one trigger onSelection of any XRGrabbableInteractor
        _deps.selection.OnSelectionChanged += ExecuteSnap;
        _deps.selection.OnRaycastPerformed += ExecuteSnap;


        // Lock UI
        _deps.handMenu.Show(false);
        _deps.handMenu.Lock = true;
    }

    private void ExecuteSnap(RaycastHit hit)
    {
        // Block execution because the other function will be called aswell
        if (hit.collider.GetComponent<XRGrabInteractable>() != null) return;

        // Stop listening immediately!
        _deps.selection.OnSelectionChanged -= ExecuteSnap;
        _deps.selection.OnRaycastPerformed -= ExecuteSnap;

        BoxCollider snap2 = hit.collider as BoxCollider;

        if (snap2 == null)
        {
            ResetState();
            return;
        }

        // Perform Logic
        _deps.selection.ClearSelection();


        SnapFollow snapComp;
        // Update or add the target to follow
        if (!_snap1.TryGetComponent(out snapComp))
            snapComp = _snap1.AddComponent<SnapFollow>();

        snapComp.Init(snap2.transform, hit.point);

        _deps.handMenu.Show(false);

        // Clean up and Unlock
        ResetState();
    }

    private void ExecuteSnap(VRSelectionManager.SelectionChangedArgs args)
    {
        var interactable = args.selection;

        if (interactable == null)
        {
            ResetState();
            return;
        }

        // Stop listening immediately!
        _deps.selection.OnSelectionChanged -= ExecuteSnap;
        _deps.selection.OnRaycastPerformed -= ExecuteSnap;


        // Perform Logic
        XRGrabInteractable snap2 = interactable;

        _deps.selection.ClearSelection();


        SnapFollow snapComp;
        // Update or add the target to follow
        if (!_snap1.TryGetComponent(out snapComp))
            snapComp = _snap1.AddComponent<SnapFollow>();

        snapComp.Init(snap2.transform, args.contactPoint);

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

        _snap1 = null;

        // Unlock Hand
        _deps.handMenu.Lock = false;

        // Ensure we don't have lingering listeners (safe to call even if not subscribed)
        _deps.selection.OnSelectionChanged -= ExecuteSnap;
    }

    private void SetupTutorialTXT()
    {
        _tutorialText.gameObject.SetActive(true);
        _tutorialText.transform.SetParent(_snap1.transform);
        var snap1BC = _snap1.GetComponent<BoxCollider>();
        _tutorialText.transform.localPosition = snap1BC.center;
        var textComp = _tutorialText.GetComponent<TextMeshPro>();
        textComp.fontSize = snap1BC.size.MinComponent() * textScaleFactor;
        textComp.fontSize = Mathf.Clamp(textComp.fontSize, minFont, maxFont);
    }

    // Usually called if the user switches tools or closes the menu manually
    public override void OnRemove()
    {
        base.OnRemove();
        ResetState();
    }
}
