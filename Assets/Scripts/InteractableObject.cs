using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableObject : Interactable
{
    private Material selectedMaterial;
    private Material baseMaterial;
    private EditorHUDView hud;

    void Awake()
    {
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        baseMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    public override void OnSelect()
    {
        gameObject.GetComponent<MeshRenderer>().material = selectedMaterial;
        if (hud == null)
            hud = FindAnyObjectByType<EditorHUDView>();
    }

    public override void OnDeselect()
    {
        if (gameObject.TryGetComponent<MeshRenderer>(out var mesh))
        {
            mesh.material = baseMaterial;
        }
    }
}

