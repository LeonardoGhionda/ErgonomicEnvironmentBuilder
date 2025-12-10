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
    private RoomEditHUD bui;

    void Awake()
    {
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        materialsMap = new();
    }

    public override void OnSelect()
    {
        BoxCollider parentCollider = gameObject.AddComponent<BoxCollider>();
        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();

        Bounds combinedLocalBounds = new(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (var mr in childrenMRs)
        {
            // compute LOCAL bounds
            Bounds localB = mr.localBounds;

            // convert child-local bounds into parent-local space
            Matrix4x4 childToParent = gameObject.transform.worldToLocalMatrix * mr.transform.localToWorldMatrix;
            Bounds b = TransformBounds(childToParent, localB);

            if (!hasBounds)
            {
                combinedLocalBounds = b;
                hasBounds = true;
            }
            else
            {
                combinedLocalBounds.Encapsulate(b);
            }

            materialsMap.Add(mr.GetInstanceID(), mr.material);
            mr.material = selectedMaterial;
        }

        if (hasBounds)
        {
            parentCollider.center = combinedLocalBounds.center;
            parentCollider.size = combinedLocalBounds.size;
        }

        parentCollider.enabled = false;

        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();

        if (bui == null) bui = FindAnyObjectByType<RoomEditHUD>();
        bui.OpenSelectionPanel(rgt);
    }

    static Bounds TransformBounds(Matrix4x4 m, Bounds b)
    {
        Vector3 center = m.MultiplyPoint3x4(b.center);

        // extents transform via absolute matrix
        Vector3 extents = b.extents;
        Vector3 axisX = m.MultiplyVector(new Vector3(extents.x, 0, 0));
        Vector3 axisY = m.MultiplyVector(new Vector3(0, extents.y, 0));
        Vector3 axisZ = m.MultiplyVector(new Vector3(0, 0, extents.z));

        extents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
        );

        return new Bounds(center, 2f * extents);
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

        var collVisual = gameObject.GetNamedChild(RuntimeGizmoTransform.colliderVisualName);
        Destroy(collVisual);
        Destroy(gameObject.GetComponent<RuntimeGizmoTransform>());
        Destroy(gameObject.GetComponent<BoxCollider>());
        bui.CloseMenu();
    }
}
