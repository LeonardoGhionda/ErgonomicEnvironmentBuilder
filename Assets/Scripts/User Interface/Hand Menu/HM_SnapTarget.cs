using System;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_SnapTarget : HM_Base
{
    XRGrabInteractable _snap1;
    Renderer S1Renderer => _snap1.GetComponent<MeshRenderer>();
    [SerializeField] FollowCameraUI _tutorialText;
    [SerializeField] Material _snapSelectedMaterial;
    [SerializeField, Range(0.01f, 16f)] float textScaleFactor = 0.1f;
    [SerializeField] float minFont, maxFont;
    Material[] _originalMaterials;

    VRSelectionManager _selectionManager;
    HandMenuManager _handMenu;

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
        _selectionManager = Managers.Get<VRSelectionManager>();
        _handMenu = Managers.Get<HandMenuManager>();
    }

    public override void OnClick()
    {
        base.OnClick();

        // Safety Check: Is something actually selected?
        if (_selectionManager.Selected == null) return;

        _snap1 = _selectionManager.Selected;

        // Clear strictly AFTER saving the reference
        _selectionManager.ClearSelection();

        // Save and Change Materials
        _originalMaterials = S1Renderer.materials;
        Material[] newMats = new Material[_originalMaterials.Length];
        for (int i = 0; i < newMats.Length; i++) newMats[i] = _snapSelectedMaterial;
        S1Renderer.materials = newMats;

        // Tutorial Text Setup
        SetupTutorialTXT();

        // Subscribe
        // This one trigger onSelection of any XRGrabbableInteractor
        _selectionManager.OnSelectionChanged += ExecuteSnap;
        _selectionManager.OnRaycastPerformed += ExecuteSnap;


        // Lock UI
        _handMenu.Show(false);
        _handMenu.Lock = true;
    }

    /// <summary>
    /// This version of ExecuteSnap is for the case where we raycast on a snap point without actually selecting a grabbable.
    /// It's needed to snap walls ground and celing, which are not grabbable but should still be snap targets.
    /// </summary>
    /// <param name="hit"></param>
    private void ExecuteSnap(RaycastHit hit)
    {
        if (_snap1 == null)
        {
            ResetState();
            return;
        }

        // Block execution because the other function will be called aswell
        if (hit.collider.GetComponent<XRGrabInteractable>() != null) return;

        // Stop listening immediately!
        _selectionManager.OnSelectionChanged -= ExecuteSnap;
        _selectionManager.OnRaycastPerformed -= ExecuteSnap;

        BoxCollider snap2 = hit.collider as BoxCollider;

        if (snap2 == null)
        {
            ResetState();
            return;
        }

        // Perform Logic
        _selectionManager.ClearSelection();

        // Update or add the target to follow
        if (!_snap1.TryGetComponent(out SnapFollow snapComp))
            snapComp = _snap1.AddComponent<SnapFollow>();

        snapComp.Init(snap2.transform, hit.point);

        _handMenu.Show(false);

        // snap + gravity cause weird physics interactions.
        if (_snap1.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Clean up and Unlock
        ResetState();
    }


    /// <summary>
    /// this version of ExecuteSnap is for the case where we actually select a grabbable instead of just raycasting on a snap point. 
    /// We want to allow both options to give the user more freedom, 
    /// but we have to be careful about the order of execution since both will trigger onSelectionChanged.
    /// </summary>
    /// <param name="args"></param>
    private void ExecuteSnap(VRSelectionManager.SelectionChangedArgs args)
    {
        if (_snap1 == null)
        {
            ResetState();
            return;
        }

        XRGrabInteractable interactable = args.selection;

        if (interactable == null)
        {
            ResetState();
            return;
        }

        // Stop listening immediately!
        _selectionManager.OnSelectionChanged -= ExecuteSnap;
        _selectionManager.OnRaycastPerformed -= ExecuteSnap;


        // Perform Logic
        XRGrabInteractable snap2 = interactable;

        _selectionManager.ClearSelection();


        // Update or add the target to follow
        if (!_snap1.TryGetComponent(out SnapFollow snapComp))
            snapComp = _snap1.AddComponent<SnapFollow>();

        snapComp.Init(snap2.transform, args.contactPoint);

        _handMenu.Show(false);


        // snap + gravity cause weird physics interactions.
        if (interactable.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

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
            S1Renderer.materials = _originalMaterials;
        }

        _snap1 = null;

        // Unlock Hand
        _handMenu.Lock = false;

        // Ensure we don't have lingering listeners (safe to call even if not subscribed)
        _selectionManager.OnSelectionChanged -= ExecuteSnap;

    }

    private void SetupTutorialTXT()
    {
        _tutorialText.gameObject.SetActive(true);
        _tutorialText.transform.SetParent(_snap1.transform);
        BoxCollider snap1BC = _snap1.GetComponent<BoxCollider>();
        _tutorialText.transform.localPosition = snap1BC.center;
        TextMeshPro textComp = _tutorialText.GetComponent<TextMeshPro>();
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
