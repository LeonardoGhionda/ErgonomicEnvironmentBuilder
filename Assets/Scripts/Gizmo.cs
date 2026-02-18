using UnityEngine;

/// <summary>
/// Control handles for a transformation 
/// </summary>
public class Gizmo : MonoBehaviour
{
    [SerializeField] Transform xHandle;
    [SerializeField] Transform yHandle;
    [SerializeField] Transform zHandle;
    // Optional Center Handle (for Scale)
    [SerializeField] Transform cHandle;

    // Currently visible handles 
    Transform[] _handles;
    // Currently selected handle
    private Transform _selected;

    public bool IsHandleSelected => _selected != null;
    public Transform SelectedHandle => _selected;

    void Awake()
    {
        if (cHandle != null)
            _handles = new Transform[] { xHandle, yHandle, zHandle, cHandle };
        else
            _handles = new Transform[] { xHandle, yHandle, zHandle };
    }

    public void SetActive(bool value)
    {
        DeselectHandle();
        gameObject.SetActive(value);
    }

    public void SelectHandle(Transform handle)
    {
        foreach (var item in _handles)
            item.gameObject.SetActive(false);

        _selected = handle;
        _selected.gameObject.SetActive(true);
    }

    public void DeselectHandle()
    {
        _selected = null;
        if (_handles == null) return;

        foreach (var item in _handles)
            item.gameObject.SetActive(true);
    }

    public void SetHandlesInPosition(Transform selectedObj, bool local)
    {
        if (_handles == null)
        {
            Debug.LogError("Gizmo: Handles not initialized!");
            return;
        }

        if (selectedObj == null)
        {
            Debug.LogError("Gizmo: Selected Object is null!");
            return;
        }

        foreach (var handle in _handles)
        {
            handle.transform.position = selectedObj.position;
        }

        // Rotation depends on Local vs Global setting
        if (local)
        {
            // Local: Align with object rotation
            xHandle.up = selectedObj.right;
            yHandle.up = selectedObj.up;
            zHandle.up = selectedObj.forward;
        }
        else
        {
            // Global: Align with World Axes
            xHandle.up = Vector3.right;
            yHandle.up = Vector3.up;
            zHandle.up = Vector3.forward;
        }
    }

    public Vector3 SelectedDirection()
    {
        if (_selected == null) return Vector3.zero;
        if (_selected != xHandle &&
            _selected != yHandle &&
            _selected != zHandle) return Vector3.one;
        return _selected.up;
    }

    public Vector3 OriginalDirection()
    {
        if (_selected == xHandle)
        {
            return Vector3.right;
        }
        else if (_selected == yHandle)
        {
            return Vector3.up;
        }
        else if (_selected == zHandle)
        {
            return Vector3.forward;
        }
        else if (_selected == cHandle)
        {
            return Vector3.one;
        }
        else
        {
            return Vector3.one;
        }
    }

    public void ScaleHandles(Vector3 scale)
    {
        if (_handles == null) return;

        foreach (var handle in _handles)
        {
            handle.localScale = scale;
        }
    }
}
