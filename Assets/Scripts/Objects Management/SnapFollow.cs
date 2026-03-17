using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapFollow : MonoBehaviour
{

#if USE_XR
    private XRGrabInteractable _grabInteractable;
#endif

    private Transform _target;
    private bool _initialized;

    private Vector3 _positionOffset;
    private Quaternion _rotationOffset;

    private Vector3 _targetLocalNormal;
    private Vector3 _myLocalAxis;
    private float _pivotToFaceDistance;


    private Vector3 _lastScale;
    private Vector3 _targetLastScale;

    private BoxCollider _bcInternal = null;
    private BoxCollider BoxCollider
    {
        get
        {
            if (_bcInternal == null) _bcInternal = GetComponent<BoxCollider>();

            return _bcInternal;
        }
    }

    // Getters
    public string TargetID
    {
        get
        {
            if (_target.TryGetComponent<Interactable>(out Interactable i))
                return i.ID;
            else
                return "BUILDING/" + _target.name;

        }
    }

    public void Init(Transform t)
    {
        // Prevent recursive loops by removing existing SnapFollow on target
        if (t.TryGetComponent<SnapFollow>(out SnapFollow snapComp) && snapComp.TargetCmp(transform))
        {
            Destroy(snapComp);
        }

        _target = t;

        // Calculate contact point as the closest point on the target's BoxCollider
        Vector3 calculatedContactPoint;
        if (t.TryGetComponent<BoxCollider>(out BoxCollider box))
        {
            calculatedContactPoint = box.ClosestPoint(transform.position);
        }
        else
        {

            Debug.LogWarning($"No box collider found for {t.name}");
            // Fallback to target position if no BoxCollider is found
            calculatedContactPoint = t.position;
        }

        // Initialize scale check
        _lastScale = transform.localScale;
        _targetLastScale = _target.localScale;

        PerformInitialSnap(t, calculatedContactPoint);
    }

    public void Init(Transform t, Vector3 contactPoint)
    {
        // Prevent recursive loops by removing existing SnapFollow on target
        if (t.TryGetComponent<SnapFollow>(out SnapFollow snapComp) && snapComp.TargetCmp(transform))
        {
            Destroy(snapComp);
        }

        _target = t;

        // Initailize scale check
        _lastScale = transform.localScale;
        _targetLastScale = _target.localScale;

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
        Plane facePlane = new(targetNormal, planePoint);
        Vector3 fixedContactPoint = facePlane.ClosestPointOnPlane(contactPoint);

        // Align the object rotation to the surface normal
        Vector3 worldDirectionToAlign = transform.TransformDirection(_myLocalAxis);
        Quaternion alignmentRotation = Quaternion.FromToRotation(worldDirectionToAlign, -targetNormal);
        Quaternion rotation = alignmentRotation * transform.rotation;

        // Place object on the surface accounting for pivot offset
        Vector3 position = fixedContactPoint + (targetNormal * _pivotToFaceDistance);

        transform.SetPositionAndRotation(position, rotation);

        UpdateFollowOffsets();

        _initialized = true;
    }

    private void LateUpdate()
    {
        // Safety check 
        if (!_initialized || _target == null) return;

#if !USE_XR
        Debug.LogWarning($"SnapFollowComponent: missing implementation for Desktop Mod");
        this.enabled = false;
        return;
#else

        if (_grabInteractable == null && TryGetComponent(out _grabInteractable) == false) return;

        // If scale change reset Snap t match box colliders
        if (!_grabInteractable.isSelected &&                                                        // Snapped is not currently grabbed 
           (!_target.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab) || !grab.isSelected) &&      // Target is not currently grabbed
            ScaleChanged())                                                                         // Scale of one of the two Transform involved changed
        {
            Init(_target, _target.GetComponent<BoxCollider>().ClosestPoint(transform.position));
            _lastScale = transform.localScale;
        }


        if (_grabInteractable.isSelected)
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
#endif
    }



    private void ConstrainMovement()
    {
        // Reconstruct plane in current world space
        Vector3 currentTargetNormal = _target.TransformDirection(_targetLocalNormal);
        BoxCollider targetBox = _target.GetComponent<BoxCollider>();
        Vector3 planePoint = GetTargetPlanePoint(_target, targetBox, _targetLocalNormal);
        Plane facePlane = new(currentTargetNormal, planePoint);

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

    private void UpdateFollowOffsets()
    {
        _positionOffset = Quaternion.Inverse(_target.rotation) * (transform.position - _target.position);
        _rotationOffset = Quaternion.Inverse(_target.rotation) * transform.rotation;
    }

    private void FollowTarget()
    {
        transform.SetPositionAndRotation(
            _target.position + _target.rotation * _positionOffset,
            _target.rotation * _rotationOffset
            );
    }

    private Vector3 GetTargetPlanePoint(Transform t, BoxCollider box, Vector3 localNormal)
    {
        Vector3 targetFaceCenterLocal = box.center + Vector3.Scale(box.size * 0.5f, localNormal);
        return t.TransformPoint(targetFaceCenterLocal);
    }

    private float GetPivotToFaceDistance(Vector3 localAxis)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 size = BoxCollider.size;
        Vector3 center = BoxCollider.center;

        float dist;

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

    public bool TargetCmp(Transform t)
    {
        return _target == t;
    }

    private bool ScaleChanged()
    {
        bool changed = transform.localScale != _lastScale || _target.localScale != _targetLastScale;
        _lastScale = transform.localScale;
        _targetLastScale = _target.localScale;
        return changed;
    }
}