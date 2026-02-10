using Dummiesman;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ScaleManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float handleSize = 0.1f; // Visual size in meters
    [SerializeField] private float handlePadding = 0.05f; // Distance from object face
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 5.0f;

    private GameObject _targetObject;
    private BoxCollider _targetCollider;
    private List<GameObject> _handles = new List<GameObject>();

    // Cache original state for Reset
    private Vector3 _originalScale;
    private Mesh _originalMesh;

    // Cache collider bounds because we disable the collider during scaling
    private Vector3 _cachedColSize;
    private Vector3 _cachedColCenter;

    private VRSelectionManager _sm;

    private void Start()
    {
        _sm = FindAnyObjectByType<VRSelectionManager>();
        _sm.OnIParentDeleted += DeleteModFile;
    }

    /// <summary>
    /// When an InteractableParent is destroyed check if it was a 
    /// Non uniform scale mod and if so delete the file
    /// </summary>
    /// <param name="parent"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void DeleteModFile(InteractableParent parent)
    {
        if (parent.Path.Contains("#m"))
        {
            File.Delete(parent.Path);

            //Save room because if we delete the file and keep the room the same by quitting without saving, room will be corrupted 
            RoomsUtility.Save(FindAnyObjectByType<RoomBuilderManager>().RoomName);
        }
    }

    public void StartScaling(GameObject target)
    {
        if (target == null) return;
        _targetObject = target;
        _targetCollider = target.GetComponent<BoxCollider>();

        if (_targetCollider == null)
        {
            Debug.LogError("ScaleManager: Target requires a BoxCollider.");
            return;
        }

        _originalScale = target.transform.localScale;
        _originalMesh = target.GetComponent<MeshFilter>().sharedMesh;

        // Cache collider data
        _cachedColSize = _targetCollider.size;
        _cachedColCenter = _targetCollider.center;

        // Disable Target Interaction and Collider to prevent raycast blocking
        if (_targetObject.TryGetComponent<XRGrabInteractable>(out var grab)) grab.enabled = false;
        _targetCollider.enabled = false;

        GenerateHandles();
    }

    private void GenerateHandles()
    {
        CreateSingleHandle(AxisHandle.Axis.X, Color.red);
        CreateSingleHandle(AxisHandle.Axis.Y, Color.green);
        CreateSingleHandle(AxisHandle.Axis.Z, Color.blue);
    }

    private void CreateSingleHandle(AxisHandle.Axis axis, Color color)
    {
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = $"ScaleHandle_{axis}";
        handle.transform.SetParent(_targetObject.transform, false);

        // Set visual properties
        var rend = handle.GetComponent<Renderer>();
        rend.material.color = color;
        if (rend.material.shader.name == "Standard")
            rend.material.SetFloat("_Glossiness", 0f);

        // Disable trigger to ensure raycasts detect the handle
        var col = handle.GetComponent<BoxCollider>();
        col.isTrigger = false;

        // Add kinematic rigidbody to prevent physics forces
        var rb = handle.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Configure XR interaction
        var interactable = handle.AddComponent<XRGrabInteractable>();
        interactable.trackPosition = false;
        interactable.trackRotation = false;
        interactable.throwOnDetach = false;

        // Manually assign collider to interactable to prevent initialization delay
        interactable.colliders.Clear();
        interactable.colliders.Add(col);

        // Copy interaction layers from target to ensure controller visibility
        if (_targetObject.TryGetComponent<XRGrabInteractable>(out var targetGrab))
        {
            interactable.interactionLayers = targetGrab.interactionLayers;
        }
        else
        {
            interactable.interactionLayers = -1;
        }

        // Match the physics layer of the target
        handle.layer = _targetObject.layer;

        // Attach logic script
        var axisScript = handle.AddComponent<AxisHandle>();
        axisScript.Setup(_targetObject.transform, axis, minScale, maxScale);

        _handles.Add(handle);
    }

    void Update()
    {
        if (_targetObject == null || _handles.Count == 0) return;

        Vector3 parentScale = _targetObject.transform.localScale;

        // Determine uniform scale based on the smallest axis of the parent
        float minAxis = Mathf.Min(parentScale.x, Mathf.Min(parentScale.y, parentScale.z));
        float currentSize = handleSize * minAxis;
        float currentPadding = handlePadding * minAxis;

        foreach (var handle in _handles)
        {
            var script = handle.GetComponent<AxisHandle>();

            // Calculate inverse scale to keep handles cubic while growing with the object
            // The handle grows uniformly based on the parent max axis
            handle.transform.localScale = new Vector3(
                currentSize / parentScale.x,
                currentSize / parentScale.y,
                currentSize / parentScale.z
            );

            // Calculate position using cached collider data
            Vector3 newPos = _cachedColCenter;
            float offset = 0.5f;

            if (script.TargetAxis == AxisHandle.Axis.X)
            {
                offset = (_cachedColSize.x * 0.5f) + (currentPadding / parentScale.x);
                newPos.x += offset;
            }
            else if (script.TargetAxis == AxisHandle.Axis.Y)
            {
                offset = (_cachedColSize.y * 0.5f) + (currentPadding / parentScale.y);
                newPos.y += offset;
            }
            else
            {
                // Z Axis
                offset = (_cachedColSize.z * 0.5f) + (currentPadding / parentScale.z);
                newPos.z += offset;
            }

            handle.transform.localPosition = newPos;
        }
    }

    public void ConfirmScale()
    {
        if (_targetObject == null) return;

        // Bake vertices
        var mf = _targetObject.GetComponent<MeshFilter>();
        mf.BakeCurrentScale();

        // Reset the transform scale to 1 because the scale is now baked into the mesh vertices
        _targetObject.transform.localScale = Vector3.one;

        // Update the real collider to match new mesh bounds
        _targetCollider.size = mf.sharedMesh.bounds.size;
        _targetCollider.center = mf.sharedMesh.bounds.center;

        SaveAsNewOBJ();
        Cleanup();
    }

    public void ResetScale()
    {
        if (_targetObject == null) return;

        // Revert changes
        _targetObject.transform.localScale = _originalScale;
        _targetObject.GetComponent<MeshFilter>().sharedMesh = _originalMesh;

        Cleanup();
    }

    private void Cleanup()
    {
        foreach (var h in _handles) Destroy(h);
        _handles.Clear();

        if (_targetObject != null)
        {
            if (_targetObject.TryGetComponent<XRGrabInteractable>(out var grab)) grab.enabled = true;
            if (_targetCollider != null) _targetCollider.enabled = true;
        }

        _targetObject = null;
    }

    private void SaveAsNewOBJ()
    {
        // Create a new model folder to contain the new mesh 
        InteractableParent iParent = _targetObject.GetComponentInParent<InteractableParent>();

        string ogFileName =
            Path.GetFileNameWithoutExtension(iParent.Path);

        int pos = ogFileName.IndexOf('#');
        if (pos != -1) // Object was already a mod 
        {
            // Remove mod id if present
            ogFileName = ogFileName.Substring(0, pos);
        }

        string ogDirPath =
        Path.GetDirectoryName(iParent.Path);

        int i = 0;
        string newFileName = "";
        string newFilePath = "";
        string modID = "";

        do
        {
            modID = $"#m{++i}.obj";
            newFileName = string.Concat(ogFileName, modID);
            newFilePath = Path.Combine(ogDirPath, newFileName); // Same folder but different OBJ file 
        } while (File.Exists(newFilePath));

        // Delete old file 
        DeleteModFile(iParent);

        //Update this parent path with the new OBJFile path
        iParent.Path = newFilePath;
        // Update parent name 
        iParent.gameObject.name = string.Concat(iParent.gameObject.name, modID);


        // Save mesh
        OBJExporter.Export(iParent);
    }
}