using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PivotManager : MonoBehaviour
{
    // Config
    public bool PivotAtCenter { get; set; }

    // State
    private XRGrabInteractable _target;
    public XRGrabInteractable Target
    {
        set { ChangeTarget(value); }
        get { return _target; }
    }

    private Transform _originalParent;
    private bool _isGrabbing = false;
    private bool _wasKinematic;
    private Rigidbody _targetRb;

    // We keep a direct reference to the wrapper to ensure we can always destroy it
    private GameObject _currentWrapper;

    private void Start()
    {
        PivotAtCenter = false;
    }

    private void LateUpdate()
    {
        // 1. Don't update position if we are currently controlling the rotation
        if (_target == null || _isGrabbing) return;

        // 2. Position Logic
        if (PivotAtCenter)
        {
            // Check if collider exists to avoid errors
            var col = _target.GetComponent<BoxCollider>();
            if (col != null)
                transform.position = _target.transform.TransformPoint(col.center);
            else
                transform.position = _target.transform.position;
        }
        else
        {
            transform.position = _target.transform.position;
        }

        // 3. Scale Logic
        // Use a safe minimum scale to prevent errors
        Vector3 lossyScale = _target.transform.lossyScale;
        float minComponent = Mathf.Min(lossyScale.x, lossyScale.y, lossyScale.z);

        float scale = minComponent / 2;
        scale = Mathf.Clamp(scale, 0f, .1f);
        transform.localScale = Vector3.one * scale;
    }

    private void ChangeTarget(XRGrabInteractable interactable)
    {
        // Safety Check: If we are currently grabbing the old target, force a clean release first.
        if (_isGrabbing && _target != null)
        {
            ForceCleanupWrapper();
        }

        // Unsubscribe from previous target
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

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (_target == null) return;

        _isGrabbing = true;
        _targetRb = _target.GetComponent<Rigidbody>();

        // 1. Save original state
        _originalParent = _target.transform.parent;
        if (_targetRb) _wasKinematic = _targetRb.isKinematic;

        // 2. Create the wrapper and save reference
        _currentWrapper = new GameObject("TempPivotWrapper");
        _currentWrapper.transform.position = transform.position; // Position at pivot
        _currentWrapper.transform.rotation = _target.transform.rotation; // Match rotation


        // 3. Parent target to wrapper
        _target.transform.SetParent(_currentWrapper.transform, true);

        // 4. Disable standard XR tracking on the target to let us control it
        _target.trackPosition = false;
        _target.trackRotation = false;

        // 5. Start rotating
        StartCoroutine(RotateWrapperRoutine(_currentWrapper.transform, args.interactorObject.transform));
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        ForceCleanupWrapper();
    }

    /// <summary>
    /// Restores the target to its original state and destroys the wrapper.
    /// Can be called safely from OnRelease, ChangeTarget, or OnDisable.
    /// </summary>
    private void ForceCleanupWrapper()
    {
        StopAllCoroutines();

        // Restore target if it still exists
        if (_target != null && _currentWrapper != null)
        {
            // Check if the target is actually inside our wrapper before reparenting
            if (_target.transform.parent == _currentWrapper.transform)
            {
                _target.transform.SetParent(_originalParent, true);
            }

            _target.trackPosition = true;
            _target.trackRotation = true;

            if (_targetRb)
            {
                _targetRb.isKinematic = _wasKinematic;
                // Stop momentum on release if it wasn't kinematic
                if (!_wasKinematic) _targetRb.linearVelocity = Vector3.zero;
            }
        }

        // Always destroy the wrapper
        if (_currentWrapper != null)
        {
            Destroy(_currentWrapper);
            _currentWrapper = null;
        }

        _isGrabbing = false;
    }

    private IEnumerator RotateWrapperRoutine(Transform wrapper, Transform hand)
    {
        if (wrapper == null || hand == null) yield break;

        // Calculate initial offset rotation
        Quaternion initialHandRot = hand.rotation;
        Quaternion initialWrapperRot = wrapper.rotation;

        while (_isGrabbing && wrapper != null && hand != null)
        {
            // Rotate the wrapper based on hand delta
            Quaternion handDelta = hand.rotation * Quaternion.Inverse(initialHandRot);
            wrapper.rotation = handDelta * initialWrapperRot;

            // Sync visual pivot to wrapper
            transform.position = wrapper.position;

            yield return null;
        }
    }

    // Safety: If this script is disabled or the object destroyed, clean up immediately
    private void OnDisable()
    {
        if (_isGrabbing)
        {
            ForceCleanupWrapper();
        }
    }
}