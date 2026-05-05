using System;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;

public class HM_SetAttachPoint : HM_Base
{
    Transform attachPointObject, target;
    [SerializeField] FollowCameraUI _tutorialText;
    [SerializeField, Range(0.01f, 16f)] float textScaleFactor = 0.1f;
    [SerializeField] float minFont, maxFont;

    VRSelectionManager _selectionManager;
    HandMenuManager _handMenu;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (attachPointObject != null)
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
        if (_selectionManager.Selected == null) return;
        base.OnClick();

        attachPointObject = _selectionManager.Selected.transform;
        _selectionManager.ClearSelection(skipCallback: true);
        if (attachPointObject == null) return;

        SetupTutorialTXT();
        _handMenu.Show(false);
        _handMenu.Lock = true;

        _selectionManager.OnSelectionChanged += OnFirstSelection;
    }

    private void OnFirstSelection(VRSelectionManager.SelectionChangedArgs args)
    {
        _selectionManager.OnSelectionChanged -= OnFirstSelection;
        if (args == null || args.selection == null)
        {
            ResetState();
            return;
        }
        target = args.selection.transform;
        _selectionManager.OnSelectionChanged += OnSecondSelection;
    }

    private void OnSecondSelection(VRSelectionManager.SelectionChangedArgs args)
    {
        if (args == null) return;
        if (args.selection != null) // target changed
        {
            target = args.selection.transform;
            return;
        }

        _selectionManager.OnSelectionChanged -= OnSecondSelection;

        if (target == null) return;

        // Create isolated child object for the trigger
        GameObject attachPointGO = new($"AttachPoint_{target.name}");
        attachPointGO.transform.SetParent(attachPointObject, false);

        attachPointGO.AddComponent<AttachPoint>().Setup(target);

        ResetState();
    }

    private void ResetState()
    {
        if (_tutorialText != null)
        {
            _tutorialText.transform.SetParent(null);
            _tutorialText.gameObject.SetActive(false);
        }

        attachPointObject = null;
        _handMenu.Lock = false;
    }

    private void SetupTutorialTXT()
    {
        _tutorialText.gameObject.SetActive(true);
        _tutorialText.transform.SetParent(attachPointObject.transform);

        BoxCollider snap1BC = attachPointObject.GetComponent<BoxCollider>();
        _tutorialText.transform.localPosition = snap1BC.center;

        TextMeshPro textComp = _tutorialText.GetComponent<TextMeshPro>();
        textComp.fontSize = snap1BC.size.MinComponent() * textScaleFactor;
        textComp.fontSize = Mathf.Clamp(textComp.fontSize, minFont, maxFont);
    }

    public override void OnRemove()
    {
        base.OnRemove();
        ResetState();
    }
}