using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PivotManager : MonoBehaviour
{
    [SerializeField] VRSelectionManager selectionManager;

    // Config
    public bool PivotAtCenter { get; set; }

    // State
    private XRGrabInteractable _target;
    public XRGrabInteractable Target { set { ChangeTarget(value); }  get{  return _target; } }
    private Transform _originalParent;
    private bool _isGrabbing = false;
    private bool _wasKinematic;
    private Rigidbody _targetRb;

    private void Start()
    {
        PivotAtCenter = false;
    }

    private void LateUpdate()
    {
        // 1. Don't update position if we are currently controlling the rotation
        //    (The hand controls it now)
        if (_target == null || _isGrabbing) return;

        // 2. Position Logic
        if (PivotAtCenter)
        {
            transform.position = _target.transform.TransformPoint(_target.GetComponent<BoxCollider>().center);
        }
        else
        {
            transform.position = _target.transform.position;
        }

        // 3. Scale Logic
        float scale = _target.transform.lossyScale.MinComponent() / 2;
        scale = Mathf.Clamp(scale, 0f, .1f);
        transform.localScale = Vector3.one * scale;
    }

    private void ChangeTarget(XRGrabInteractable interactable)
    {
        // Unsubscribe from previous target to avoid memory leaks
        if (_target != null)
        {
            _target.selectEntered.RemoveListener(OnGrab);
            _target.selectExited.RemoveListener(OnRelease);
        }

        _target = interactable;
        gameObject.SetActive(_target != null);

        // Subscribe to new target
        if (_target != null)
        {
            _target.selectEntered.AddListener(OnGrab);
            _target.selectExited.AddListener(OnRelease);
        }
    }

    // --- Dynamic PivotManager Logic ---

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbing = true;
        _targetRb = _target.GetComponent<Rigidbody>();

        // 1. Save original state
        _originalParent = _target.transform.parent;
        if (_targetRb) _wasKinematic = _targetRb.isKinematic;

        // 2. Create a temporary wrapper at THIS pivot's position
        //    We use a new object so we don't mess up scales.
        GameObject wrapper = new GameObject("TempPivotWrapper");
        wrapper.transform.position = transform.position; // Position at the pivot sphere
        wrapper.transform.rotation = _target.transform.rotation; // Match object rotation

        // 3. Parent the target to the wrapper
        _target.transform.SetParent(wrapper.transform, true);

        // 4. Disable standard XR tracking on the target
        _target.trackPosition = false;
        _target.trackRotation = false;

        // 5. Start rotating the wrapper based on the hand
        StartCoroutine(RotateWrapperRoutine(wrapper.transform, args.interactorObject.transform));
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        StopAllCoroutines(); // Stop the rotation loop

        // 1. Get the wrapper before we unparent
        Transform wrapper = _target.transform.parent;

        // 2. Restore Target Hierarchy
        _target.transform.SetParent(_originalParent, true);

        // 3. Restore Target Settings
        _target.trackPosition = true;
        _target.trackRotation = true;

        if (_targetRb)
        {
            _targetRb.isKinematic = _wasKinematic;

            if(!_wasKinematic) _targetRb.linearVelocity = Vector3.zero;
        }

        // 4. Destroy the wrapper
        if (wrapper != null && wrapper.name == "TempPivotWrapper")
        {
            Destroy(wrapper.gameObject);
        }

        _isGrabbing = false;
    }

    private IEnumerator RotateWrapperRoutine(Transform wrapper, Transform hand)
    {
        // Calculate initial offset rotation
        Quaternion initialHandRot = hand.rotation;
        Quaternion initialWrapperRot = wrapper.rotation;

        while (_isGrabbing && wrapper != null)
        {
            // Rotate the wrapper based on how much the hand turned
            Quaternion handDelta = hand.rotation * Quaternion.Inverse(initialHandRot);
            wrapper.rotation = handDelta * initialWrapperRot;

            // Sync this visual PivotManager sphere to the wrapper so it doesn't lag behind
            transform.position = wrapper.position;

            yield return null;
        }
    }
}