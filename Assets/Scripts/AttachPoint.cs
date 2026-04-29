using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static UnityEngine.GraphicsBuffer;

public class AttachPoint : MonoBehaviour
{
    [SerializeField] float Radius = 0.05f;
    [SerializeField] float AngleThreshold = 20f;

    [SerializeField] Transform _target;
    string _targetName = "";
    CapsuleCollider _collider;
    Vector3 _posOffset;
    Quaternion _rotOffset = Quaternion.identity;

    public string TargetName => _targetName;
    public Vector3 PosOffset => _posOffset;
    public Quaternion RotOffset => _rotOffset;

    void OnEnable()
    {
        _collider = gameObject.AddComponent<CapsuleCollider>();
        _collider.isTrigger = true;
        _collider.height = Radius * 2;
        _collider.radius = Radius;
    }

    void OnDisable()
    {
        if (_collider != null)
        {
            Destroy(_collider);
        }
    }

    public void Setup(Transform target)
    {
        _targetName = target.name;

        BoxCollider targetCollider = target.GetComponent<BoxCollider>();
        Vector3 boxColliderCenterOffset = target.TransformPoint(targetCollider.center);
        _posOffset = transform.InverseTransformPoint(boxColliderCenterOffset);

        _rotOffset = Quaternion.Inverse(transform.rotation) * target.rotation;

        _collider.center = _posOffset;
    }

    public void Setup(AttachPointData attachPointData)
    {
        _targetName = attachPointData.targetName;
        _posOffset = attachPointData.posOffset;
        _rotOffset = attachPointData.rotOffset;
        _collider.center = _posOffset;
    }

    void Update()
    {
        if (_target != null)
        {
            _target.rotation = transform.rotation * _rotOffset;

            if (!_target.TryGetComponent<BoxCollider>(out var targetCollider)) Destroy(this);

            Vector3 worldCenterOffset = _target.TransformPoint(targetCollider.center) - _target.position;

            _target.position = transform.TransformPoint(_posOffset) - worldCenterOffset;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_target != null) return;

        if (!string.IsNullOrEmpty(_targetName) && (other.name.Contains(_targetName) || _targetName.Contains(other.name)))
        {
            Quaternion expectedRotation = transform.rotation * _rotOffset;

            if (Quaternion.Angle(other.transform.rotation, expectedRotation) <= AngleThreshold)
            {
                _target = other.transform;
                _target.rotation = expectedRotation;

                if (_target.TryGetComponent<XRGrabInteractable>(out var grabObj))
                {
                    if (grabObj.isSelected)
                    {
                        grabObj.interactionManager.SelectCancel(grabObj.firstInteractorSelecting, grabObj);
                    }
                }

                if (Managers.Get<StateManager>().CmpState(typeof(RoomTestState)))
                {
                    LockTarget();
                }
                else
                {
                    Debug.Log($"Current state is not test");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_target != null && other.transform == _target)
        {
            _target = null;
        }
    }

    private void LockTarget()
    {
        // Disable gravity 
        if (_target.TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        //Remove snap follow
        Destroy(_target.GetComponent<SnapFollow>());

        if(_target.TryGetComponent<InteractableObject>(out var intObj)) intObj.Locked = true;
        else Debug.LogWarning($"Target {_target.name} does not have InteractableObject component, cannot set Locked to true");
    }
}