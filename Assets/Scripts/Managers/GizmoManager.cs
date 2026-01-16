using System;
using System.Buffers;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TransformMode
{
    Translate = 0,
    Rotate,
    Scale
}

public class GizmoManager : MonoBehaviour
{
    #region Inspector Variable
    [Header("Translate")]
    [SerializeField] Gizmo Translate;

    [Header("Rotate")]
    [SerializeField] Gizmo Rotate;

    [Header("Scale")]
    [SerializeField] Gizmo Scale;
    #endregion

    #region Getter
    public Gizmo[] All => new Gizmo[] { Translate, Rotate, Scale };
    public TransformMode TransformMode => _tMode;
    public bool LocalTransform => _localTransform;

    public bool SelectedMoved()
    {
        var returnValue = _objMoved;
        _objMoved = false;
        return returnValue;
    }
    #endregion

    #region Setters
    public void SetMode(TransformMode newMode, Transform selectedObject)
    {
        // Check and switch
        if (newMode == _tMode && _currentGizmo != null) return;

        _tMode = newMode;

        // Change current gizmo
        if (_currentGizmo != null)
            _currentGizmo.SetActive(false);

        switch (_tMode)
        {
            case TransformMode.Translate:
                _currentGizmo = Translate;
                break;
            case TransformMode.Rotate:
                _currentGizmo = Rotate;
                break;
            case TransformMode.Scale:
                _currentGizmo = Scale;
                break;
        }
        _currentGizmo.SetActive(true);
        _currentGizmo.SetHandlesInPosition(selectedObject, _localTransform);
    }

    public void SetLocal(bool value, Transform selected)
    {
        _localTransform = value;
        // Scale is always local
        if (_tMode == TransformMode.Scale) _localTransform = true;

        if (_currentGizmo != null)
            _currentGizmo.SetHandlesInPosition(selected, _localTransform);
    }
    #endregion

    #region Static Variables
    public static readonly int GizmoLayer = 7; // Ensure this Layer exists in Unity Settings
    #endregion

    #region Private Variables

    // State of gizmo
    private TransformMode _tMode = TransformMode.Translate;
    private bool _localTransform;

    // Mouse wrapping variables
    private int _mouseWrapMarginX;
    private int _mouseWrapMarginY;
    
    private bool _firstDrag = true;
    private Vector2 _lastMousePos;

    private Gizmo _currentGizmo;

    private MeasureSnapTools _snapTool = new();

    private bool _objMoved = false;

    // Cache to avoid GC allocations
    private RaycastHit[] _raycastHitsCache = new RaycastHit[16];
    #endregion

    #region Injected variables
    Camera _cam;
    CameraController _camController;
    #endregion

    #region Lifecycle


    /// <summary>
    /// To be called when manager start
    /// </summary>
    public void Init(Camera cam, CameraController cameraController)
    {
        enabled = true;

        _cam = cam; 
        _camController = cameraController;

        _tMode = TransformMode.Translate;
        _localTransform = true; // Default Local

        // setup
        _mouseWrapMarginX = Screen.width / 200;
        _mouseWrapMarginY = Screen.height / 200;

    }

    /// <summary>
    /// To be called when gizmo manager is not needed anymore
    /// </summary>
    public void Stop()
    {
        foreach (var item in All)
        {
            item.SetActive(false);
        }
    }


    #endregion

    #region Handles Management 

    public void UpdateGizmoPosition(Transform selected)
    {
        if (_currentGizmo != null)
            _currentGizmo.SetHandlesInPosition(selected, _localTransform);
    }

    /// <summary>
    /// if handles are present they should be selected even if hitted behind an occlusion, 
    /// because they always appear in front of everything
    /// </summary>
    public bool TrySelectHandle(Vector2 mousePos)
    {

        Ray ray = _cam.ScreenPointToRay(mousePos);

        // Only handles are visible to the ray via LayerMask
        int mask = 1 << GizmoLayer;
        int hitCount = Physics.RaycastNonAlloc(ray, _raycastHitsCache, Mathf.Infinity, mask);

        if (hitCount == 0) return false;

        // Find closest handle
        float minDst = float.MaxValue;
        Transform bestHit = null;

        for (int i = 0; i < hitCount; i++)
        {
            if (_raycastHitsCache[i].distance < minDst)
            {
                minDst = _raycastHitsCache[i].distance;
                bestHit = _raycastHitsCache[i].transform;
            }
        }

        if (bestHit != null) 
            _currentGizmo.SelectHandle(bestHit);

        _firstDrag = true;

        return true;
    }

    public void DeselectHandle(Transform selected)
    {
        if (_currentGizmo != null)
        {
            _currentGizmo.DeselectHandle();
            _currentGizmo.SetHandlesInPosition(selected, _localTransform);
        }
    }

    #endregion

    #region Objects Transformations
    public void HandleDragging(Transform selected, Vector2 mousePos, bool snap)
    {
        //initialize last mouse pos
        if (_firstDrag)
        {
            _lastMousePos = mousePos;
            _firstDrag = false;
        }

        if (_currentGizmo == null || !_currentGizmo.IsHandleSelected) return;

        // Mouse wrapping logic (infinite drag)
        var (warped, newPos) = WarpMouse(mousePos);
        if (warped)
        {
            mousePos = newPos;
            _lastMousePos = newPos; // avoid jump
        }

        float projected = 0f;
        float dot = Mathf.Abs(Vector3.Dot(_cam.transform.forward.normalized, _currentGizmo.SelectedDirection().normalized));
        float trashold = 0.02f;
        // the other method won't work well when the axis is almost perpendicular to the camera view direction
        if (dot >= 1f - trashold && dot <= 1f + trashold)
        {
            //right positive / left negative 
            float screenSign = mousePos.x > Screen.width / 2 ? 1 : -1;

            //farther positive / closer negative
            Vector2 objScreenPos = _cam.WorldToScreenPoint(selected.position);
            float moveSign = Vector2.Distance(objScreenPos, mousePos) >
                             Vector2.Distance(objScreenPos, _lastMousePos) ? 1 : -1;

            projected = Vector2.Distance(mousePos, _lastMousePos) * screenSign * moveSign;
        }
        else
        {
            Vector2 delta = mousePos - _lastMousePos;

            // Projection calculations
            Vector3 screenP0 = _cam.WorldToScreenPoint(selected.position);
            Vector3 screenP1 = _cam.WorldToScreenPoint(selected.position + _currentGizmo.SelectedDirection());
            Vector2 axisScreen = (screenP1 - screenP0).normalized;

            // Scalar amount of movement along the axis
            projected = Vector2.Dot(delta, axisScreen);

        }

        // Make movement in world space proportional to distance from camera
        float distance = (selected.position - _cam.transform.position).magnitude;
        float worldScale = distance * 0.001f;

        // Apply transform
        switch (_tMode)
        {
            case TransformMode.Translate:
                ApplyTranslation(selected, projected, worldScale);
                if(snap && _snapTool.TrySnap(selected)) DeselectHandle(selected);
                break;
            case TransformMode.Rotate:
                ApplyRotation(selected, projected, worldScale);
                break;
            case TransformMode.Scale:
                ApplyScale(selected, projected, worldScale);
                break;
        }

        _objMoved = true;
        _lastMousePos = mousePos;

        _currentGizmo.SetHandlesInPosition(selected, _localTransform);
    }

    private void ApplyTranslation(Transform selected, float projected, float worldScale)
    {
        Vector3 moveDir = _currentGizmo.SelectedDirection();
        Vector3 translation = moveDir * (projected * worldScale);
        selected.Translate(translation, Space.World);
    }

    private void ApplyRotation(Transform selected, float projected, float worldScale)
    {
        float angle = projected * worldScale * 20f; // Sensitivity multiplier
        // Rotate around handle axis
        selected.Rotate(_currentGizmo.SelectedDirection(), -angle, Space.World);
    }

    private void ApplyScale(Transform selected, float projected, float worldScale)
    {
        float scaleAmount = projected * worldScale * 0.5f;

        Vector3 direction = _currentGizmo.OriginalDirection();

        Vector3 newScale = selected.localScale + (direction * scaleAmount);

        float minScale = 0.04f;
        // Prevent negative or zero scale
        if (newScale.x <= minScale || newScale.y < minScale || newScale.z < minScale)
            return;

        selected.localScale = newScale;
    }

    #endregion

    #region Gizmo Management

    public void NewTarget(Transform selectedObject)
    {
        SetMode(_tMode, selectedObject);
        _currentGizmo.SetActive(true);
        _currentGizmo.SetHandlesInPosition(selectedObject, _localTransform);
    }

    public void RemoveGizmo()
    {
        if (_currentGizmo != null)
        {
            _currentGizmo.SetActive(false);
            _currentGizmo = null;
        }
    }

    public void ScaleHandlesByCameraDistance(Transform selected)
    {
        if (_currentGizmo == null) return;
        
        float scale;
        
        if (_cam.orthographic)
        {
            scale = _cam.orthographicSize * 0.3f;
        }
        else
        {
            float dist = Vector3.Distance(selected.position, _cam.transform.position);
            scale = dist * 0.2f;
        }

        Vector3 scale3 = Vector3.one * scale;
        _currentGizmo.ScaleHandles(scale3);

    }

    #endregion

    #region Utility

    private (bool, Vector2) WarpMouse(Vector2 pos)
    {
        bool warped = false;
        if (pos.x < _mouseWrapMarginX) { pos.x = Screen.width - _mouseWrapMarginX * 2; warped = true; }
        else if (pos.x > Screen.width - _mouseWrapMarginX) { pos.x = _mouseWrapMarginX * 2; warped = true; }

        if (pos.y < _mouseWrapMarginY) { pos.y = Screen.height - _mouseWrapMarginY * 2; warped = true; }
        else if (pos.y > Screen.height - _mouseWrapMarginY) { pos.y = _mouseWrapMarginY * 2; warped = true; }

        if (warped) Mouse.current.WarpCursorPosition(pos);
        return (warped, pos);
    }
    #endregion
}