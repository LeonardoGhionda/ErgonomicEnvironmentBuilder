using System;
using UnityEngine;

public class AttachPoint : MonoBehaviour
{
    [SerializeField] float Radius = 0.05f;

    [SerializeField] Transform _target;
    string _targetName = "";
    CapsuleCollider _collider;
    Vector3 _posOffset;
    Quaternion _rotOffset = Quaternion.identity;


    public string TargetName => _targetName;
    public Vector2 PosOffset => _posOffset;
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

            if (!_target.TryGetComponent<BoxCollider>(out var targetCollider)) Destroy(this); // Safety check

            Vector3 worldCenterOffset = _target.TransformPoint(targetCollider.center) - _target.position;

            _target.position = transform.TransformPoint(_posOffset) - worldCenterOffset;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_target == null && !string.IsNullOrEmpty(_targetName))
        {
            if (other.name.Contains(_targetName) || _targetName.Contains(other.name))
            {
                _target = other.transform;
                _target.rotation = transform.rotation * _rotOffset;

                if (_target.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = true;
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
}