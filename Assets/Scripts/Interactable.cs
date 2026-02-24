using System;
using UnityEngine;

abstract public class Interactable : MonoBehaviour
{
    private Guid _idInternal;
    public string ID
    {
        get
        {
            if (_idInternal == null || _idInternal == Guid.Empty) _idInternal = Guid.NewGuid();
            return _idInternal.ToString();
        }
        set
        {
            if (!string.IsNullOrEmpty(value)) _idInternal = new Guid(value);
        }
    }

    abstract public void OnSelect();
    abstract public void OnDeselect();
}
