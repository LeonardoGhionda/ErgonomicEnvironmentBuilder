using System;
using UnityEngine;

abstract public class Interactable : MonoBehaviour
{

    protected virtual void Awake()
    {
        _idInternal = Guid.NewGuid();
    }

    private Guid _idInternal;
    public string ID { get { return _idInternal.ToString(); } set { _idInternal = new(value); } }

    abstract public void OnSelect();
    abstract public void OnDeselect();
}
