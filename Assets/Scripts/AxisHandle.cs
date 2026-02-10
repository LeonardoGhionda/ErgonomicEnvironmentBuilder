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

    // Stores initial state
    private Vector3 _initialHandPosLocal;
    private Vector3 _initialTargetScale;
    private bool _isDragging;

    public void Setup(Transform target, Axis axis, float min, float max)
    {
        _target = target;
        TargetAxis = axis;
        _min = min;
        _max = max;

        var interactable = GetComponent<XRGrabInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject;
        _isDragging = true;
        _initialTargetScale = _target.localScale;

        // Calculate hand position relative to the target object pivot
        _initialHandPosLocal = _target.InverseTransformPoint(_interactor.transform.position);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isDragging = false;
        _interactor = null;
    }

    void Update()
    {
        if (!_isDragging || _target == null || _interactor == null) return;

        Vector3 currentHandPosLocal = _target.InverseTransformPoint(_interactor.transform.position);
        Vector3 newScale = _target.localScale;

        float ratio = 1.0f;

        // Calculate scale based on axis
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