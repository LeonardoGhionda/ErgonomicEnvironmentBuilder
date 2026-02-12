using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapFollow : MonoBehaviour
{
    private Transform _target;
    private XRGrabInteractable _grabInteractable;
    private bool _initialized;
    private bool _isGrabbed;

    private Vector3 _positionOffset;
    private Quaternion _rotationOffset;

    private Vector3 _targetLocalNormal;
    private Vector3 _myLocalAxis;
    private float _pivotToFaceDistance;

    private BoxCollider _bcInternal;
    private BoxCollider boxCollider => _bcInternal ??= GetComponent<BoxCollider>();

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    public void Init(Transform t, Vector3 contactPoint)
    {
        // Check for valid target and avoid self-assignment
        if (t == null || t == transform) return;

        // Prevent recursive loops by removing existing SnapFollow on target
        if (t.TryGetComponent<SnapFollow>(out var snapComp) && snapComp.targetCmp(transform))
        {
            Destroy(snapComp);
        }

        _target = t;

        // Enable tracking so user can slide the object on the surface
        if (_grabInteractable != null)
        {
            _grabInteractable.trackPosition = true;
            _grabInteractable.trackRotation = true;
            _isGrabbed = _grabInteractable.isSelected;
        }

        PerformInitialSnap(t, contactPoint);
    }

    private void PerformInitialSnap(Transform t, Vector3 contactPoint)
    {
        BoxCollider targetBox = t.GetComponent<BoxCollider>();
        Vector3 targetNormal = targetBox.ContactPointNormal(contactPoint);

        // Store target normal in local space to handle future target rotation
        _targetLocalNormal = t.InverseTransformDirection(targetNormal);
        _targetLocalNormal = SnapVectorToAxis(_targetLocalNormal);

        // Define all local axes to check against
        Vector3[] localAxes = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        Vector3 bestLocalAxis = Vector3.up;
        float minDot = 2f;

        // Find which local axis is pointing towards the target surface
        foreach (Vector3 axis in localAxes)
        {
            Vector3 worldAxis = transform.TransformDirection(axis);
            float dot = Vector3.Dot(targetNormal, worldAxis);
            if (dot < minDot)
            {
                minDot = dot;
                bestLocalAxis = axis;
            }
        }
        _myLocalAxis = bestLocalAxis;

        // Calculate geometric distance from pivot to the chosen face
        _pivotToFaceDistance = GetPivotToFaceDistance(_myLocalAxis);

        // Project contact point onto the exact plane of the target face
        Vector3 planePoint = GetTargetPlanePoint(t, targetBox, _targetLocalNormal);
        Plane facePlane = new Plane(targetNormal, planePoint);
        Vector3 fixedContactPoint = facePlane.ClosestPointOnPlane(contactPoint);

        // Align the object rotation to the surface normal
        Vector3 worldDirectionToAlign = transform.TransformDirection(_myLocalAxis);
        Quaternion alignmentRotation = Quaternion.FromToRotation(worldDirectionToAlign, -targetNormal);
        transform.rotation = alignmentRotation * transform.rotation;

        // Place object on the surface accounting for pivot offset
        transform.position = fixedContactPoint + (targetNormal * _pivotToFaceDistance);

        UpdateFollowOffsets();

        _initialized = true;
    }

    private void LateUpdate()
    {
        if (!_initialized || _target == null) return;

        if (_isGrabbed)
        {
            // Allow user movement but lock to the target plane
            ConstrainMovement();
            UpdateFollowOffsets();
        }
        else
        {
            // Stick rigidly to the target when not held
            FollowTarget();
        }
    }

    private void ConstrainMovement()
    {
        // Reconstruct plane in current world space
        Vector3 currentTargetNormal = _target.TransformDirection(_targetLocalNormal);
        BoxCollider targetBox = _target.GetComponent<BoxCollider>();
        Vector3 planePoint = GetTargetPlanePoint(_target, targetBox, _targetLocalNormal);
        Plane facePlane = new Plane(currentTargetNormal, planePoint);

        // Project current position onto the plane
        Vector3 rawPos = transform.position;
        Vector3 closestPointOnPlane = facePlane.ClosestPointOnPlane(rawPos);
        transform.position = closestPointOnPlane + (currentTargetNormal * _pivotToFaceDistance);

        // Lock rotation axis to normal while allowing twist
        Vector3 myCurrentAxisVector = transform.TransformDirection(_myLocalAxis);
        Vector3 desiredAxisVector = -currentTargetNormal;

        Quaternion correction = Quaternion.FromToRotation(myCurrentAxisVector, desiredAxisVector);
        transform.rotation = correction * transform.rotation;
    }

    private void FollowTarget()
    {
        transform.position = _target.position + _target.rotation * _positionOffset;
        transform.rotation = _target.rotation * _rotationOffset;
    }

    private void UpdateFollowOffsets()
    {
        _positionOffset = Quaternion.Inverse(_target.rotation) * (transform.position - _target.position);
        _rotationOffset = Quaternion.Inverse(_target.rotation) * transform.rotation;
    }

    private void OnGrab(SelectEnterEventArgs args) => _isGrabbed = true;
    private void OnRelease(SelectExitEventArgs args) => _isGrabbed = false;

    private Vector3 GetTargetPlanePoint(Transform t, BoxCollider box, Vector3 localNormal)
    {
        Vector3 targetFaceCenterLocal = box.center + Vector3.Scale(box.size * 0.5f, localNormal);
        return t.TransformPoint(targetFaceCenterLocal);
    }

    private float GetPivotToFaceDistance(Vector3 localAxis)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 size = boxCollider.size;
        Vector3 center = boxCollider.center;

        float dist = 0f;

        // Calculate distance based on the active axis
        if (Mathf.Abs(localAxis.x) > 0.5f)
            dist = (center.x * scale.x) + (Mathf.Sign(localAxis.x) * size.x * 0.5f * scale.x);
        else if (Mathf.Abs(localAxis.y) > 0.5f)
            dist = (center.y * scale.y) + (Mathf.Sign(localAxis.y) * size.y * 0.5f * scale.y);
        else
            dist = (center.z * scale.z) + (Mathf.Sign(localAxis.z) * size.z * 0.5f * scale.z);

        return Mathf.Abs(dist);
    }

    private Vector3 SnapVectorToAxis(Vector3 v)
    {
        float x = Mathf.Abs(v.x);
        float y = Mathf.Abs(v.y);
        float z = Mathf.Abs(v.z);

        if (x > y && x > z) return new Vector3(Mathf.Sign(v.x), 0, 0);
        if (y > x && y > z) return new Vector3(0, Mathf.Sign(v.y), 0);
        return new Vector3(0, 0, Mathf.Sign(v.z));
    }

    public bool targetCmp(Transform t)
    {
        return _target == t;
    }
}