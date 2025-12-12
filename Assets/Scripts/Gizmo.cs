using System;
using System.Linq;
using UnityEngine;

public class Gizmo : MonoBehaviour
{
    Handle[] _handles;
    private Handle _selected;

    void Start()
    {
        _handles = GetComponentsInChildren<Handle>();
    }

    public void SetActive(bool value)
    {
        DeselectHandle();
        gameObject.SetActive(value);
    }

    public void SelectHandle(Handle handle)
    {
        if(!_handles.Contains(handle))
        {
            Debug.LogError("Handle not part of gizmo");
        }

        foreach (var item in _handles)
            item.gameObject.SetActive(false);

        _selected = handle;
        _selected.gameObject.SetActive(true);
    }

    public void DeselectHandle()
    {
        _selected?.gameObject.SetActive(false);
        _selected = null;
    }
}
