using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class PivotBasedRotation : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The Transform that marks the center of rotation (e.g., your Sphere)")]
    [SerializeField] private Transform pivotCenter;

    private XRGrabInteractable _interactable;
    private Transform _originalParent;
    private GameObject _tempPivotWrapper;
    private bool _wasKinematic;
    private Rigidbody _rb;

    private void Awake()
    {
        _interactable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        // Safety check
        if (pivotCenter == null)
        {
            Debug.LogError("Pivot Center is missing! Please assign the Sphere transform.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _interactable.selectEntered.AddListener(OnGrab);
        _interactable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnGrab);
        _interactable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // 1. Save original state
        _originalParent = transform.parent;
        if (_rb) _wasKinematic = _rb.isKinematic;

        // 2. Create the temporary wrapper at the Pivot's position
        _tempPivotWrapper = new GameObject($"{gameObject.name}_PivotWrapper");
        _tempPivotWrapper.transform.position = pivotCenter.position;
        _tempPivotWrapper.transform.rotation = transform.rotation;

        // 3. Parent this object to the wrapper
        // The object stays visually in place, but now moves relative to the wrapper
        transform.SetParent(_tempPivotWrapper.transform, true);

        // 4. Disable standard XR tracking on THIS object
        // We don't want the XR script moving the child directly anymore
        _interactable.trackPosition = false;
        _interactable.trackRotation = false;

        // 5. Start our custom update loop
        // We need to manually rotate the WRAPPER based on the hand
        StartCoroutine(TrackHand(args.interactorObject.transform));
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        StopAllCoroutines();

        // 1. Unparent (Restore original hierarchy)
        transform.SetParent(_originalParent, true);

        // 2. Destroy the wrapper
        if (_tempPivotWrapper != null)
        {
            Destroy(_tempPivotWrapper);
        }

        // 3. Restore XR settings
        _interactable.trackPosition = true; // Or whatever your default was
        _interactable.trackRotation = true;

        // 4. Restore Physics (Optional)
        if (_rb)
        {
            _rb.isKinematic = _wasKinematic;
            _rb.velocity = Vector3.zero; // Stop any drift
        }
    }

    private System.Collections.IEnumerator TrackHand(Transform hand)
    {
        // Capture the initial offset between Hand and Wrapper
        Quaternion initialHandRot = hand.rotation;
        Quaternion initialWrapperRot = _tempPivotWrapper.transform.rotation;

        while (true)
        {
            // Calculate how much the hand has rotated since the start
            Quaternion handDelta = hand.rotation * Quaternion.Inverse(initialHandRot);

            // Apply that rotation to the Wrapper (which rotates the child around the pivot)
            _tempPivotWrapper.transform.rotation = handDelta * initialWrapperRot;

            yield return null;
        }
    }
}