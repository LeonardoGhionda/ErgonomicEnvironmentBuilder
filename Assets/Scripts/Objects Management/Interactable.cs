using System;
using System.Linq;
using UnityEngine;

abstract public class Interactable : MonoBehaviour
{
    private Guid _idInternal;
    public string ID
    {
        get => _idInternal.ToString();
        set => _idInternal = new Guid(value);
    }

    protected virtual void Awake()
    {
        // Generate a new ID only if one hasn't been assigned yet
        if (_idInternal == Guid.Empty)
        {
            _idInternal = Guid.NewGuid();
        }
    }

    abstract public void OnSelect();
    abstract public void OnDeselect();

    public static Interactable FindByID(string ID)
    {
        GameObject objContainer = GameObject.Find("Objects Container");
        if (objContainer == null)
        {
            Debug.LogError("Objects Container not found in current scene");
            return null;
        }

        // Return the first match or null if not found
        return objContainer.GetComponentsInChildren<Interactable>()
            .FirstOrDefault(i => i.ID == ID);
    }
}