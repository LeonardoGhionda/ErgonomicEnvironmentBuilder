using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableParent : Interactable
{
    public string path;
    private Material selectedMaterial;
    private Dictionary<int, Material> materialsMap;
    private BuildingUi bui;

    void Awake()
    {
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        materialsMap = new();
    }

    public override void OnSelect()
    {
        BoxCollider parentCollider = gameObject.AddComponent<BoxCollider>();
        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();

        Bounds combinedBounds = new Bounds(gameObject.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var mr in childrenMRs)
        {
            //calculate bounds
            if (!hasBounds)
            {
                //bound initialization
                combinedBounds = mr.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(mr.bounds);
            }

            //set material
            materialsMap.Add(mr.GetInstanceID(), mr.material);
            mr.material = selectedMaterial;
        }

        if (hasBounds)
        {
            Vector3 localSize = gameObject.transform.InverseTransformVector(combinedBounds.size);
            Vector3 localCenter = gameObject.transform.InverseTransformPoint(combinedBounds.center);

            parentCollider.size = localSize;
            parentCollider.center = localCenter;
        }

        parentCollider.enabled = false;

        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();

        if (bui == null) bui = FindAnyObjectByType<BuildingUi>();
        bui.OpenSelectionPanel(rgt);
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
