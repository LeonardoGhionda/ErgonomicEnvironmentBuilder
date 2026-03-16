using System;
using System.Linq;
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

    public static Interactable FindByID(string ID)
    {
        GameObject objContainer = GameObject.Find("Objects Container");
        if (objContainer != null) throw new Exception("Objects Container not found in current scene");
        Interactable result = objContainer.GetComponentsInChildren<Interactable>().First(i => i.ID == ID);
        return result;
    }
}
