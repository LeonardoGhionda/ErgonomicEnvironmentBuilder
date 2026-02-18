using System.Collections.Generic;
using UnityEngine;

public class InteractableParent : Interactable
{

    private string path;
    public string Path
    {
        get { return path; }
        set { path = value; }
    }

    private Material _selectedMaterial;
    private Dictionary<int, Material[]> _materialsMap;

    void Awake()
    {
        _selectedMaterial = Resources.Load<Material>("Materials/TransparentGreen");
        _materialsMap = new();
        // Set layer to Ignore Raycast to avoid children "hiding"
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public override void OnSelect()
    {
        var children = gameObject.GetComponentsInChildren<InteractableObject>();
        if (children.Length == 0) return;

        Bounds visualBounds = new Bounds(children[0].transform.position, Vector3.zero);

        // variables for tight bounds calculation
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        // Loop 1: Calculate Center & Detach
        foreach (var child in children)
        {
            if (child.TryGetComponent(out MeshRenderer mr))
            {
                // Material save and swap
                _materialsMap[mr.GetInstanceID()] = mr.materials;
                Material[] highlightMaterials = new Material[mr.materials.Length];
                for (int i = 0; i < highlightMaterials.Length; i++)
                {
                    highlightMaterials[i] = _selectedMaterial;
                }
                mr.materials = highlightMaterials;
                visualBounds.Encapsulate(mr.bounds);
            }
            child.transform.SetParent(null);
        }

        // move parent to center
        transform.position = visualBounds.center;

        // Loop 2: Reattach & Calculate Tight Size
        foreach (var child in children)
        {
            child.transform.SetParent(transform);

            // Calculate tight bounds here (relative to the new parent center)
            if (child.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                Vector3 c = b.center, e = b.extents;

                // 8 corners of the raw mesh
                Vector3[] pts = {
                    c + new Vector3(e.x, e.y, e.z), c + new Vector3(e.x, e.y, -e.z),
                    c + new Vector3(e.x, -e.y, e.z), c + new Vector3(e.x, -e.y, -e.z),
                    c + new Vector3(-e.x, e.y, e.z), c + new Vector3(-e.x, e.y, -e.z),
                    c + new Vector3(-e.x, -e.y, e.z), c + new Vector3(-e.x, -e.y, -e.z)
                };

                foreach (var p in pts)
                {
                    // transform point to parent local space
                    Vector3 localPt = transform.InverseTransformPoint(child.transform.TransformPoint(p));
                    min = Vector3.Min(min, localPt);
                    max = Vector3.Max(max, localPt);
                }
            }
        }

        // Add collider
        BoxCollider parentCollider = gameObject.AddComponent<BoxCollider>();
        if (parentCollider != null)
        {
            parentCollider.center = Vector3.zero;
            // if max < min (no mesh found), fallback to visual bounds size
            parentCollider.size = (max.x > min.x) ? (max - min) : visualBounds.size;
        }
    }

    public override void OnDeselect()
    {

        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in childrenMRs)
        {
            if (_materialsMap.TryGetValue(mr.GetInstanceID(), out Material[] materials))
                mr.materials = materials;
        }
        _materialsMap.Clear();
        Destroy(gameObject.GetComponent<BoxCollider>());
    }

}
