using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableParent : Interactable
{

    private string path;
    public string Path
    {
        get { return path;  }
        set { path = value; }
    }

    private Material selectedMaterial;
    private Dictionary<int, Material> materialsMap;

    void Awake()
    {
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        materialsMap = new();
    }

    public override void OnSelect()
    {
        Debug.Log("Parent Selected: " + gameObject.name);

        var children = gameObject.GetComponentsInChildren<InteractableObject>();
        Bounds combinedBounds = new Bounds(children[0].transform.position, Vector3.zero);

        foreach (var child in children)
        {
            // Update material
            if(child.TryGetComponent(out MeshRenderer mr))
            {
                materialsMap[mr.GetInstanceID()] = mr.material;
                mr.material = selectedMaterial;
            }

            //Calculate bounds 
            if (child.TryGetComponent(out Renderer rend))
                combinedBounds.Encapsulate(rend.bounds);

            // detach children to avoid parent transform influence during move
            child.transform.SetParent(null);
        }

        // move parent to the exact center
        transform.position = combinedBounds.center;

        // reattach children
        foreach (var child in children)
            child.transform.SetParent(transform);

        BoxCollider parentCollider = gameObject.AddComponent<BoxCollider>();
        if (parentCollider != null)
        {
            // center is zero because we just moved the transform to the bounds center
            parentCollider.center = Vector3.zero;

            // calculate correct local size (handles rotation)
            parentCollider.size = GetLocalBoundsSize(transform, combinedBounds);
        }

    }

    // helper to convert world bounds to local size
    private Vector3 GetLocalBoundsSize(Transform t, Bounds worldBounds)
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        Vector3 center = worldBounds.center;
        Vector3 ext = worldBounds.extents;

        // get the 8 corners of the world box
        Vector3[] worldCorners = new Vector3[]
        {
        center + new Vector3( ext.x,  ext.y,  ext.z),
        center + new Vector3( ext.x,  ext.y, -ext.z),
        center + new Vector3( ext.x, -ext.y,  ext.z),
        center + new Vector3( ext.x, -ext.y, -ext.z),
        center + new Vector3(-ext.x,  ext.y,  ext.z),
        center + new Vector3(-ext.x,  ext.y, -ext.z),
        center + new Vector3(-ext.x, -ext.y,  ext.z),
        center + new Vector3(-ext.x, -ext.y, -ext.z),
        };

        // convert to local space
        foreach (var p in worldCorners)
        {
            Vector3 localP = t.InverseTransformPoint(p);
            min = Vector3.Min(min, localP);
            max = Vector3.Max(max, localP);
        }

        return max - min;
    }

    public override void OnDeselect()
    {

        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in childrenMRs)
        {
            if (materialsMap.TryGetValue(mr.GetInstanceID(), out Material material))
                mr.material = material;
        }
        materialsMap.Clear();
        Destroy(gameObject.GetComponent<BoxCollider>());
    }

}
