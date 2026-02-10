using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AxisHandle : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public Axis TargetAxis { get; private set; }

    private Transform _target;
    private float _min, _max;
    private IXRSelectInteractor _interactor;
    private ScaleManager _manager; // Reference to the manager

    private Vector3 _initialHandPosLocal;
    private Vector3 _initialTargetScale;
    private bool _isDragging;

    // Components for visibility toggling
    private Renderer _renderer;
    private Collider _collider;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<BoxCollider>();
    }

    public void Setup(ScaleManager manager, Transform target, Axis axis, float min, float max)
    {
        _manager = manager;
        _target = target;
        TargetAxis = axis;
        _min = min;
        _max = max;

        var interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    public void SetVisibility(bool isVisible)
    {
        if (_renderer) _renderer.enabled = isVisible;
        if (_collider) _collider.enabled = isVisible;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject;
        _isDragging = true;
        _initialTargetScale = _target.localScale;
        _initialHandPosLocal = _target.InverseTransformPoint(_interactor.transform.position);

        // Notify manager to hide other handles
        if (_manager != null) _manager.OnHandleDragStart(this);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isDragging = false;
        _interactor = null;

        // Notify manager to show other handles
        if (_manager != null) _manager.OnHandleDragEnd();
    }

    void Update()
    {
        if (!_isDragging || _target == null || _interactor == null) return;

        Vector3 currentHandPosLocal = _target.InverseTransformPoint(_interactor.transform.position);
        Vector3 newScale = _target.localScale;

        float ratio = 1.0f;

        switch (TargetAxis)
        {
            case Axis.X:
                if (Mathf.Abs(_initialHandPosLocal.x) > 0.001f)
                    ratio = currentHandPosLocal.x / _initialHandPosLocal.x;
                newScale.x = Mathf.Clamp(_initialTargetScale.x * ratio, _min, _max);
                break;

            case Axis.Y:
                if (Mathf.Abs(_initialHandPosLocal.y) > 0.001f)
                    ratio = currentHandPosLocal.y / _initialHandPosLocal.y;
                newScale.y = Mathf.Clamp(_initialTargetScale.y * ratio, _min, _max);
                break;

            case Axis.Z:
                if (Mathf.Abs(_initialHandPosLocal.z) > 0.001f)
                    ratio = currentHandPosLocal.z / _initialHandPosLocal.z;
                newScale.z = Mathf.Clamp(_initialTargetScale.z * ratio, _min, _max);
                break;
        }

        _target.localScale = newScale;
    }
}