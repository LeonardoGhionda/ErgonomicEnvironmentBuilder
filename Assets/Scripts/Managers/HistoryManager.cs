using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public struct HistoryEntry
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public Interactable Target { get; set; }

    public void Undo()
    {
        if (Target != null)
        {
            Target.transform.position = Position;
            Target.transform.rotation = Rotation;
            Target.transform.localScale = Scale;
        }
    }

    public string GetSummary() => $"[Base] {Target.name} at {Position}";
}

public class HistoryManager : MonoBehaviour
{
    private Stack<HistoryEntry> _history;
    private InputAction _undoAction;

    private Interactable _currentTarget;

    private Vector3 _lastStablePosition;
    private Quaternion _lastStableRotation;
    private Vector3 _lastStableScale;

    private Vector3 _previousFramePosition;
    private Quaternion _previousFrameRotation;
    private Vector3 _previousFrameScale;

    private bool _isTrackingChange;

    private GizmoManager _gizmoManager;



    private void Start()
    { 
        _history = new Stack<HistoryEntry>();
        _gizmoManager = Managers.Get<GizmoManager>();
        SubscribeToSelectionManager();
    }

    private void OnEnable()
    {
        _undoAction = DependencyProvider.Input.History.Undo;
        _undoAction.performed += ExecuteUndo;

        DependencyProvider.Input.History.Enable();
    }

    private void OnDisable()
    {
        DependencyProvider.Input.History.Disable();
    }

    private void OnDestroy()
    {
        _undoAction.performed -= ExecuteUndo;
        UnsubscribeFromSelectionManager();
    }

    void Update()
    {
        if (_currentTarget == null) return;

        Transform targetTransform = _currentTarget.transform;

        bool isMovingThisFrame = targetTransform.position != _previousFramePosition ||
                                 targetTransform.rotation != _previousFrameRotation ||
                                 targetTransform.localScale != _previousFrameScale;

        if (isMovingThisFrame)
        {
            _isTrackingChange = true;
        }
        else if (_isTrackingChange && !isMovingThisFrame)
        {
            RecordState(_lastStablePosition, _lastStableRotation, _lastStableScale);

            _lastStablePosition = targetTransform.position;
            _lastStableRotation = targetTransform.rotation;
            _lastStableScale = targetTransform.localScale;
            _isTrackingChange = false;
        }

        _previousFramePosition = targetTransform.position;
        _previousFrameRotation = targetTransform.rotation;
        _previousFrameScale = targetTransform.localScale;
    }

    private void SetupNewTarget(Interactable interactable)
    {
        if (_currentTarget != null && _isTrackingChange)
        {
            RecordState(_lastStablePosition, _lastStableRotation, _lastStableScale);
        }

        _currentTarget = interactable;
        _isTrackingChange = false;

        if (_currentTarget != null)
        {
            Transform targetTransform = _currentTarget.transform;

            _lastStablePosition = targetTransform.position;
            _lastStableRotation = targetTransform.rotation;
            _lastStableScale = targetTransform.localScale;

            _previousFramePosition = targetTransform.position;
            _previousFrameRotation = targetTransform.rotation;
            _previousFrameScale = targetTransform.localScale;

            Debug.Log($"New target is {_currentTarget.name}");
        }


    }

    private void RecordState(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        HistoryEntry entry = new HistoryEntry
        {
            Position = pos,
            Rotation = rot,
            Scale = scale,
            Target = _currentTarget
        };

        if (_history.Count > 0)
        {
            HistoryEntry lastEntry = _history.Peek();

            bool isSameTarget = lastEntry.Target == entry.Target;

            // Using SqrMagnitude is faster than Distance because it avoids a square root calculation
            // A threshold of 0.0001 represents roughly a 1 centimeter difference in standard Unity scale
            bool isPositionSame = Vector3.SqrMagnitude(lastEntry.Position - entry.Position) < 0.001f;

            // Quaternion.Angle safely handles 360 degree wrap-arounds
            // A threshold of 0.1 degrees is virtually unnoticeable to the eye
            bool isRotationSame = Quaternion.Angle(lastEntry.Rotation, entry.Rotation) < 0.1f;

            bool isScaleSame = Vector3.SqrMagnitude(lastEntry.Scale - entry.Scale) < 0.001f;

            if (isSameTarget && isPositionSame && isRotationSame && isScaleSame)
            {
                return;
            }
        }

        _history.Push(entry);

        Debug.Log(entry.GetSummary());

    }

    private void ExecuteUndo(InputAction.CallbackContext context)
    {
        // [DT Profile] Clear the gizmo to prevent it from trying to manipulate a target that just got moved back
        // (VR profile compliant) 
        _gizmoManager.RemoveGizmo();
        Debug.Log($"Undo Performed");
        if (_history.Count > 0)
        {
            HistoryEntry entry = _history.Pop();
            entry.Undo();

            if (_currentTarget == entry.Target)
            {
                Transform targetTransform = _currentTarget.transform;

                _lastStablePosition = targetTransform.position;
                _lastStableRotation = targetTransform.rotation;
                _lastStableScale = targetTransform.localScale;

                _previousFramePosition = targetTransform.position;
                _previousFrameRotation = targetTransform.rotation;
                _previousFrameScale = targetTransform.localScale;

                _isTrackingChange = false;
            }
        }
    }

    private void SubscribeToSelectionManager()
    {
#if USE_XR
        Managers.Get<VRSelectionManager>().OnInteractableChanged += SetupNewTarget; 
#else
        Managers.Get<DTSelectionManager>().OnInteractableChanged += SetupNewTarget;
#endif
    }

    private void UnsubscribeFromSelectionManager()
    {
#if USE_XR
        var vrManager = Managers.Get<VRSelectionManager>();
        if (vrManager != null)
        {
            vrManager.OnInteractableChanged -= SetupNewTarget;
        }
#else
        var dtManager = Managers.Get<DTSelectionManager>();
        if (dtManager != null)
        {
            dtManager.OnInteractableChanged -= SetupNewTarget;
        }
#endif
    }
}