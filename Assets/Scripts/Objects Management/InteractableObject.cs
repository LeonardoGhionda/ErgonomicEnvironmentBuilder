using UnityEngine;

public class InteractableObject : Interactable
{
    private Material _selectedMaterial;
    private Material[] _baseMaterials;
    private EditorHUDView hud;

    InteractableParent _parent;
    public InteractableParent Parent => _parent;

    protected override void Awake()
    {
        base.Awake();
        _selectedMaterial = Resources.Load<Material>("Materials/TransparentGreen");
        _baseMaterials = gameObject.GetComponent<MeshRenderer>().materials;
    }

    protected override void Start()
    {
        base.Start();
        _parent = GetComponentInParent<InteractableParent>();
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
        if (gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer mesh))
        {
            mesh.materials = _baseMaterials;
        }
    }
}

