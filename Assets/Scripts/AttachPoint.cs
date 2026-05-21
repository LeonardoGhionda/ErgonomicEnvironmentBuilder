using UnityEngine;

public class AttachPoint : MonoBehaviour
{
    [SerializeField] float Radius = 0.025f;
    [SerializeField] float MaxRotationAngleDifference = 90f;

    private Transform _target;
    private BoxCollider _targetBox;

    string _targetName = "";
    CapsuleCollider _collider;
    Vector3 _posOffset;
    Quaternion _rotOffset = Quaternion.identity;

    public string TargetName => _targetName;
    public Vector3 PosOffset => _posOffset;
    public Quaternion RotOffset => _rotOffset;

    private VRSelectionManager _selectionManager;

    private void Awake()
    {
        _selectionManager = Managers.Get<VRSelectionManager>();
    }

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

        _targetBox = target.GetComponent<BoxCollider>();
        Vector3 boxColliderCenterOffset = target.TransformPoint(_targetBox.center);
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

            Vector3 worldCenterOffset = _target.TransformPoint(_targetBox.center) - _target.position;

            _target.position = transform.TransformPoint(_posOffset) - worldCenterOffset;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_target != null) return;
        if (_selectionManager.SelectionExist && _selectionManager.Selected == gameObject) return;

        if (!string.IsNullOrEmpty(_targetName) && other.name == _targetName)
        {
            Quaternion expectedRotation = transform.rotation * _rotOffset;

            // Abort snap if the current rotation is too far from the expected target rotation
            if (Quaternion.Angle(other.transform.rotation, expectedRotation) > MaxRotationAngleDifference) return;

            // Force object release if held so it can snap properly
            if (_target == null)
            {
                _selectionManager.ClearSelection();
            }

            _target = other.transform;
            _targetBox = _target.GetComponent<BoxCollider>();

            if (_targetBox == null)
            {
                Debug.LogWarning($"Target {_target.name} does not have a BoxCollider");
                return;
            }

            _target.rotation = expectedRotation;

            if (Managers.Get<StateManager>().CmpState(typeof(RoomTestState)))
            {
                LockTarget();
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
        if (_target.TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Destroy(_target.GetComponent<SnapFollow>());

        if (_target.TryGetComponent<InteractableObject>(out var intObj)) intObj.Locked = true;
        else Debug.LogWarning($"Target {_target.name} does not have InteractableObject component, cannot set Locked to true");
    }
}