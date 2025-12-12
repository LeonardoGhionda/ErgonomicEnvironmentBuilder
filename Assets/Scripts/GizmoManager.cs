using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GizmoType
{
    Translate = 0,
    Rotate,
    Scale
}

public class GizmoManager : MonoBehaviour
{
    #region Variables

    // State of gizmo
    public GizmoType TransformType => _transformType;
    private GizmoType _transformType;
    private bool _localTransform;
    public bool LocalTransform => _localTransform;

    // Mouse wrapping variables
    private int _mouseWrapMarginX;
    private int _mouseWrapMarginY;
    private Vector2 _lastMousePos;

    [SerializeField] private Transform objectContainer;

    // Switchable handlers resources 
    private Dictionary<GizmoType, Mesh> _meshes;
    private Dictionary<Vector3, Material> _materials;

    // Handles
    private Transform _xHandle, _yHandle, _zHandle, _sHandle;
    // sHandle is the one that in SCALE mode make user perform uniform scaling

    // currently dragged handle
    private Transform _currentHandle;

    // Visual constants
    public static string ColliderVisualName { get { return "Collider Visual"; } }
    public static readonly int GizmoLayer = 7; // Ensure this Layer exists in Unity Settings!

    private bool _handlesEnabled;

    // Cache to avoid GC allocations
    private RaycastHit[] _raycastHitsCache = new RaycastHit[16];

    #endregion

    #region Lifecycle

    /// <summary>
    /// To be called when manager start
    /// </summary>
    public void Init()
    {
        enabled = true;

        _transformType = GizmoType.Translate;
        _localTransform = false; // Default Global

        // setup
        _mouseWrapMarginX = Screen.width / 200;
        _mouseWrapMarginY = Screen.height / 200;

        // load resources
        _meshes = new Dictionary<GizmoType, Mesh>
        {
            { GizmoType.Translate, Resources.Load<Mesh>("Handles/Translate") },
            { GizmoType.Rotate, Resources.Load<Mesh>("Handles/Rotation") },
            { GizmoType.Scale, Resources.Load<Mesh>("Handles/Scale") }
        };

        _materials = new Dictionary<Vector3, Material>
        {
            {Vector3.right, Resources.Load<Material>("Materials/Red_AlwaysOnTop")},    // x, red
            {Vector3.up, Resources.Load<Material>("Materials/Green_AlwaysOnTop")},     // y, green
            {Vector3.forward, Resources.Load<Material>("Materials/Blue_AlwaysOnTop")}, // z, blue
        };

        RebuildHandles();
        ShowHandles(false);
    }

    /// <summary>
    /// Update call every frame
    /// </summary>
    public void GizmoUpdate(Camera cam, Vector2 mousePos)
    {
        if (Dragging())
        {
            HandleDragging(cam, mousePos);
        }

        ScaleHandlesByCameraDistance(cam);
    }

    /// <summary>
    /// To be called when gizmo manager is not needed anymore
    /// </summary>
    public void Stop()
    {
        _currentHandle = null;
        _meshes.Clear();
        _materials.Clear();
        DestroyAllHandles();
        enabled = false;
    }

    public void onRemovedSelection()
    {
        ShowHandles(false);
    }

    public void onNewSelection(Transform selection)
    {
        SetHandlesInPosition(selection);
        ShowHandles(true);
    }

    /// <summary>
    /// Check if the user is trying to start an handle drag
    /// </summary>
    /// <returns>true if handle has begin</returns>
    public bool onStartDrag(Camera cam, Vector2 mousePos)
    {
        if (!TrySelectHandle(cam, mousePos)) return false;
        HideNonDraggedHandle();

        return true;
    }

    public void onEndDragging(Transform selection)
    {
        if (!Dragging()) return;

        SetHandlesInPosition(selection);
        ShowHandles(true);
        _currentHandle = null; // end dragging
    }

    public void onSelectionExternallyMoved(Transform selection)
    {
        SetHandlesInPosition(selection);
    }

    #endregion

    #region Handles Management 

    private void ShowHandles(bool value)
    {
        _handlesEnabled = value;

        if (_xHandle) _xHandle.gameObject.SetActive(value);
        if (_yHandle) _yHandle.gameObject.SetActive(value);
        if (_zHandle) _zHandle.gameObject.SetActive(value);
        if (_sHandle) _sHandle.gameObject.SetActive(_transformType == GizmoType.Scale ? value : false);
    }

    private void HideNonDraggedHandle()
    {
        if (!Dragging()) return;
        if (_xHandle && _xHandle != _currentHandle) _xHandle.gameObject.SetActive(false);
        if (_yHandle && _yHandle != _currentHandle) _yHandle.gameObject.SetActive(false);
        if (_zHandle && _zHandle != _currentHandle) _zHandle.gameObject.SetActive(false);
        if (_sHandle && _sHandle != _currentHandle) _sHandle.gameObject.SetActive(false);
    }

    private void SetHandlesInPosition(Transform selected)
    {
        if (!_xHandle) return;

        // Position always matches selection
        _xHandle.position = selected.position;
        _yHandle.position = selected.position;
        _zHandle.position = selected.position;
        if (_sHandle) _sHandle.position = selected.position;

        // Rotation depends on Local vs Global setting
        if (_localTransform)
        {
            // Local: Align with object rotation
            _xHandle.rotation = selected.rotation * Quaternion.LookRotation(Vector3.right, Vector3.up);
            _yHandle.rotation = selected.rotation * Quaternion.LookRotation(Vector3.up, Vector3.forward);
            _zHandle.rotation = selected.rotation * Quaternion.LookRotation(Vector3.forward, Vector3.up);

            // Force exact Up vector alignment (Safer)
            _xHandle.up = selected.right;
            _yHandle.up = selected.up;
            _zHandle.up = selected.forward;
        }
        else
        {
            // Global: Align with World Axes
            _xHandle.rotation = Quaternion.identity; // Reset
            _yHandle.rotation = Quaternion.identity;
            _zHandle.rotation = Quaternion.identity;

            _xHandle.up = Vector3.right;
            _yHandle.up = Vector3.up;
            _zHandle.up = Vector3.forward;
        }

        if (_sHandle) _sHandle.rotation = selected.rotation;
    }

    /// <summary>
    /// if handles are present they should be selected even if hitted behind an occlusion, 
    /// because they always appear in front of everything
    /// </summary>
    private bool TrySelectHandle(Camera cam, Vector2 mousePos)
    {
        Ray ray = cam.ScreenPointToRay(mousePos);

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

        _currentHandle = bestHit;
        return true;
    }

    private void HandleDragging(Camera cam, Vector2 mousePos)
    {
        // Mouse wrapping logic (infinite drag)
        var (warped, newPos) = WarpMouse(mousePos);
        if (warped)
        {
            mousePos = newPos;
            _lastMousePos = newPos; // avoid jump
        }

        Vector2 delta = mousePos - _lastMousePos;

        // Projection calculations
        Vector3 screenP0 = cam.WorldToScreenPoint(transform.position);
        Vector3 screenP1 = cam.WorldToScreenPoint(transform.position + _currentHandle.up);
        Vector2 axisScreen = (screenP1 - screenP0).normalized;

        // Scalar amount of movement along the axis
        float projected = Vector2.Dot(delta, axisScreen);

        // Make movement in world space proportional to distance from camera
        float distance = (transform.position - cam.transform.position).magnitude;
        float worldScale = distance * 0.001f;

        // Fix for Ortho Camera
        if (cam.orthographic)
        {
            worldScale = cam.orthographicSize * 0.002f;
            // Fix direction inversion if handle is "behind" pivot in ortho
            if (Vector3.Dot(cam.transform.forward, _currentHandle.up) > 0)
                projected *= 1; // Logic placeholder if needed
        }

        // Apply transform
        switch (_transformType)
        {
            case GizmoType.Translate:
                ApplyTranslation(projected, worldScale);
                break;
            case GizmoType.Rotate:
                ApplyRotation(projected, worldScale);
                break;
            case GizmoType.Scale:
                ApplyScale(projected, worldScale);
                break;
        }

        _lastMousePos = mousePos;
    }

    #endregion

    #region Transformations

    private void ApplyTranslation(float projected, float worldScale)
    {
        Vector3 moveDir = _currentHandle.up;
        Vector3 translation = moveDir * (projected * worldScale);
        transform.Translate(translation, Space.World);
    }

    private void ApplyRotation(float projected, float worldScale)
    {
        float angle = projected * worldScale * 20f; // Sensitivity multiplier
        // Rotate around handle axis
        transform.Rotate(_currentHandle.up, -angle, Space.World);
    }

    private void ApplyScale(float projected, float worldScale)
    {
        float scaleAmount = projected * worldScale * 0.5f;

        if (_currentHandle == _sHandle)
        {
            // Uniform Scale
            float factor = 1 + scaleAmount;
            transform.localScale *= factor;
        }
        else
        {
            // Axial Scale
            Vector3 scaleAxis = Vector3.zero;

            // Check identity based on reference or logic
            if (_currentHandle == _xHandle) scaleAxis = Vector3.right;
            else if (_currentHandle == _yHandle) scaleAxis = Vector3.up;
            else if (_currentHandle == _zHandle) scaleAxis = Vector3.forward;

            Vector3 newScale = transform.localScale + (scaleAxis * scaleAmount);

            // Avoid zero scale
            if (Mathf.Abs(newScale.x) < 0.01f) newScale.x = 0.01f;
            if (Mathf.Abs(newScale.y) < 0.01f) newScale.y = 0.01f;
            if (Mathf.Abs(newScale.z) < 0.01f) newScale.z = 0.01f;

            transform.localScale = newScale;
        }
    }

    #endregion

    #region Handle Lifecycle

    private void RebuildHandles()
    {
        DestroyAllHandles();
        CreateHandles();
    }

    private void CreateHandles()
    {
        Mesh mesh = _meshes[_transformType];

        // Create handles for X, Y, Z
        _xHandle = CreateSingleHandle(mesh, Vector3.right);
        _yHandle = CreateSingleHandle(mesh, Vector3.up);
        _zHandle = CreateSingleHandle(mesh, Vector3.forward);
        _sHandle = CreateUniformScaleHandle();

        // If not in Scale mode, hide the center cube
        if (_sHandle) _sHandle.gameObject.SetActive(_transformType == GizmoType.Scale);

        // Position them correctly immediately if we have a parent/target
        if (transform.parent != null)
            SetHandlesInPosition(transform);

        ShowHandles(true);
    }

    private Transform CreateSingleHandle(Mesh mesh, Vector3 direction)
    {
        string name = "Error";
        if (direction == Vector3.right) name = "X";
        else if (direction == Vector3.up) name = "Y";
        else if (direction == Vector3.forward) name = "Z";

        // GameObject setup
        GameObject go = new GameObject($"Handle_{name}");
        go.layer = GizmoLayer;
        go.transform.SetParent(objectContainer);
        go.transform.position = transform.position;
        go.transform.up = direction; // Orient mesh

        // Mesh
        var filter = go.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        // Renderer
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.material = _materials[direction];

        // Collider: Use MeshCollider for Rotate (Rings), Capsule for Translate/Scale
        if (_transformType == GizmoType.Rotate)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh; // Precise raycast on the ring
            // Note: If raycast fails, ensure 'convex' is false (ok for static raycast) 
            // or true (if using Physics triggers)
        }
        else
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.1f;
            col.height = 2f;
            col.direction = 1; // Y-Axis (matches transform.up)
        }

        return go.transform;
    }

    private Transform CreateUniformScaleHandle()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Handle_Uniform";
        go.layer = GizmoLayer;
        go.transform.SetParent(objectContainer);
        go.transform.position = transform.position;
        go.transform.localScale = Vector3.one * 0.1f; // Small center cube

        Destroy(go.GetComponent<BoxCollider>()); // Remove default
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        var renderer = go.GetComponent<Renderer>();
        renderer.material = Resources.Load<Material>("Materials/Magenta_AlwaysOnTop"); // Ensure this material exists

        return go.transform;
    }

    private void DestroyAllHandles()
    {
        ShowHandles(false);
        if (_xHandle) Destroy(_xHandle.gameObject);
        if (_yHandle) Destroy(_yHandle.gameObject);
        if (_zHandle) Destroy(_zHandle.gameObject);
        if (_sHandle) Destroy(_sHandle.gameObject);

        _xHandle = null;
        _yHandle = null;
        _zHandle = null;
        _sHandle = null;
    }

    #endregion

    #region Exposed Functions

    public void SetMode(GizmoType newMode, Transform selected)
    {
        if (newMode == _transformType) return;

        _transformType = newMode;

        // Rebuild to match correct colliders and meshes
        if (enabled) RebuildHandles();
        SetHandlesInPosition(selected);
    }

    public void SetLocal(bool value, Transform selected)
    {
        _localTransform = value;

        // Scale is always local
        if (_transformType == GizmoType.Scale) _localTransform = true;

        // Just reposition, no need to rebuild
        if (enabled) SetHandlesInPosition(selected);
    }

    public bool Dragging() => _currentHandle != null;

    public void ScaleHandlesByCameraDistance(Camera cam)
    {
        if (!_handlesEnabled || !_xHandle) return;

        float scale;
        if (cam.orthographic)
        {
            scale = cam.orthographicSize * 0.3f;
        }
        else
        {
            float dist = Vector3.Distance(transform.position, cam.transform.position);
            scale = dist * 0.15f;
        }

        Vector3 scale3 = Vector3.one * scale;

        if (_xHandle) _xHandle.localScale = scale3;
        if (_yHandle) _yHandle.localScale = scale3;
        if (_zHandle) _zHandle.localScale = scale3;
        if (_sHandle) _sHandle.localScale = scale3 * 0.5f;
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