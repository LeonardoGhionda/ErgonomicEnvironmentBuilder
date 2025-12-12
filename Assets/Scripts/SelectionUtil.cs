using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public static class SelectionUtil
{
    
    /// <summary>
    /// Cast a ray to find out what user is selecting
    /// </summary>
    /// <param name="cam">Camera to cast selection ray from</param>
    /// <returns>
    /// Interactable or null if there is no collider hit
    /// </returns>
    public static Interactable GetSelection(Camera cam)
    {
        //ortho and perspective cam have 2 different ray start position
        Vector2 rayStart = cam.orthographic
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Ray ray = cam.ScreenPointToRay(rayStart);

        if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
        {
            var interactable = hit.collider.GetComponentInParent<InteractableObject>();
            return interactable;
        }
        return null;
    }

    /// <summary>
    /// <para>Delete the currently selected Interactable</para>
    /// <para>If it's a parent, delete all children too</para>
    /// <para>If it's the last children, delete the parent</para>
    /// </summary>
    public static Interactable DeleteSelected(Interactable selected, GizmoManager gizmo)
    {
        if (selected == null) return null;

        gizmo.onRemovedSelection();
        selected.OnDeselect();

        var obj = selected.gameObject;
        var newSelection = obj.transform.GetComponentInParent<InteractableParent>();

        if (newSelection != null && newSelection.gameObject.GetInstanceID() == obj.GetInstanceID())
            newSelection = null;

        // delete parent if no more childen
        if (newSelection != null)
        {
            int childrenN = newSelection.transform.GetComponentsInChildren<Interactable>().Count();
            if (childrenN <= 2)
            {
                GameObject.Destroy(newSelection.gameObject);
                newSelection = null;
            }
        }

        obj.transform.SetParent(null, false);
        GameObject.Destroy(obj);

        return ChangeSelectedObject(null, newSelection, gizmo);
    }

    public static Interactable ChangeSelectedObject(Interactable oldSelected, Interactable newSelected, GizmoManager gizmo)
    {
        // Deseleziona precedente
        if (oldSelected != null)
        {
            gizmo.onRemovedSelection();
            oldSelected.OnDeselect();
        }

        if (newSelected == null) return null;

        // Parent priority
        var parentTransform = newSelected.transform.parent;
        if (oldSelected != null && parentTransform != oldSelected.transform)
        {
            newSelected = parentTransform.GetComponent<Interactable>();
        }

        newSelected.OnSelect();
        gizmo.onNewSelection(newSelected.transform);

        return newSelected;
    }
}