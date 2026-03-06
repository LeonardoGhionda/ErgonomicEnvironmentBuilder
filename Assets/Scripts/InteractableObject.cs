using UnityEngine;

public class InteractableObject : Interactable
{
    private Material _selectedMaterial;
    private Material[] _baseMaterials;
    private EditorHUDView hud;

    protected override void Awake()
    {
        base.Awake();
        _selectedMaterial = Resources.Load<Material>("Materials/TransparentGreen");
        _baseMaterials = gameObject.GetComponent<MeshRenderer>().materials;
    }

    public override void OnSelect()
    {
        // Materials swap
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        _baseMaterials = renderer.sharedMaterials;
        Material[] highlightMaterials = new Material[_baseMaterials.Length];
        for (int i = 0; i < highlightMaterials.Length; i++)
        {
            highlightMaterials[i] = _selectedMaterial;
        }
        renderer.materials = highlightMaterials;

        // Get HUD
        if (hud == null) hud = FindAnyObjectByType<EditorHUDView>();
    }

    public override void OnDeselect()
    {
        if (gameObject.TryGetComponent<MeshRenderer>(out var mesh))
        {
            mesh.materials = _baseMaterials;
        }
    }
}

