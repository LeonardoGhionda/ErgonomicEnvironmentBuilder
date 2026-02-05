using UnityEngine;

[DisallowMultipleComponent]
public class SnapFollow : MonoBehaviour
{
    private Transform _target;

    private Vector3 _positionOffset;
    private Quaternion _rotationOffset;
    private bool _initialized;

    void LateUpdate()
    {
        if (_target == null) return;

        if (!_initialized) InitializeOffset();

        // Apply position: Target Pos + (Target Rot * Saved Offset)
        transform.position = _target.position + (_target.rotation * _positionOffset);

        // Apply rotation: Target Rot * Saved Rotation Offset
        transform.rotation = _target.rotation * _rotationOffset;
    }

    public void Init(Transform t)
    {
        _target = t;
        InitializeOffset();
    }

    private void InitializeOffset()
    {
        if (_target == null) return;

        _positionOffset = Quaternion.Inverse(_target.rotation) * (transform.position - _target.position);
        _rotationOffset = Quaternion.Inverse(_target.rotation) * transform.rotation;
        _initialized = true;
    }
}