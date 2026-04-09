using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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
    private readonly List<GameObject> _handles = new();

    // Cache original state for Reset
    private Vector3 _originalScale;
    private Mesh _originalMesh;

    // Cache collider bounds because we disable the collider during scaling
    private Vector3 _cachedColSize;
    private Vector3 _cachedColCenter;

    public void SetTarget(GameObject newTarget)
    {
        _targetObject = newTarget;
        _targetCollider = newTarget.GetComponent<BoxCollider>();
    }

    /// <summary>
    /// When an InteractableParent is destroyed check if it was a 
    /// Non uniform scale mod and if so delete the file
    /// </summary>
    private void DeleteModFile(InteractableParent parent)
    {
        if (parent.Path.Contains("#m"))
        {
            if (File.Exists(parent.Path)) File.Delete(parent.Path);

            //Save room because if we delete the file and keep the room the same by quitting without saving, room will be corrupted 
            RoomBuilderManager roomManager = FindAnyObjectByType<RoomBuilderManager>();
            if (roomManager != null) RoomManagementTools.Save(roomManager.RoomName);
        }
    }

    public void StartScaling(GameObject target)
    {
        if (target == null) return;
        
        SetTarget(target);

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
        if (_targetObject.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab)) grab.enabled = false;
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

        // Visuals - URP Compatible
        Renderer rend = handle.GetComponent<Renderer>();

        // Using the URP Lit shader
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader != null)
        {
            rend.material = new Material(urpLitShader);
        }

        rend.material.color = color;

        // In URP, Glossiness is usually handled by _Smoothness
        if (rend.material.HasProperty("_Smoothness"))
        {
            rend.material.SetFloat("_Smoothness", 0f);
        }

        // Collider
        BoxCollider col = handle.GetComponent<BoxCollider>();
        col.isTrigger = false;

        // Rigidbody (Kinematic)
        Rigidbody rb = handle.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // XR Interaction
        XRSimpleInteractable interactable = handle.AddComponent<XRSimpleInteractable>();

        // Manually assign collider
        interactable.colliders.Clear();
        interactable.colliders.Add(col);

        // Copy interaction layers from target
        if (_targetObject.TryGetComponent<XRBaseInteractable>(out XRBaseInteractable targetInteractable))
        {
            interactable.interactionLayers = targetInteractable.interactionLayers;
        }

        // Match Physics Layer
        handle.layer = _targetObject.layer;

        // Attach logic script
        AxisHandle axisScript = handle.AddComponent<AxisHandle>();
        axisScript.Setup(this, _targetObject.transform, axis, minScale, maxScale);

        _handles.Add(handle);
    }

    public void OnHandleDragStart(AxisHandle activeHandle)
    {
        foreach (GameObject handleObj in _handles)
        {
            if (handleObj == null) continue;

            // Check if this handle object is the one we are currently dragging
            bool isCurrent = (handleObj == activeHandle.gameObject);

            AxisHandle script = handleObj.GetComponent<AxisHandle>();
            if (script != null)
            {
                // Show the dragged one, hide the others
                script.SetVisibility(isCurrent);
            }
        }
    }

    public void OnHandleDragEnd()
    {
        foreach (GameObject handleObj in _handles)
        {
            if (handleObj == null) continue;

            AxisHandle script = handleObj.GetComponent<AxisHandle>();
            if (script != null)
            {
                // Restore visibility for all handles
                script.SetVisibility(true);
            }
        }
    }

    void Update()
    {
        if (_targetObject == null || _handles.Count == 0) return;

        Vector3 parentScale = _targetObject.transform.localScale;

        // Determine uniform scale based on the smallest axis of the parent
        float currentSize = handleSize;
        float currentPadding = handlePadding;

        foreach (GameObject handle in _handles)
        {
            if (handle == null) continue;
            AxisHandle script = handle.GetComponent<AxisHandle>();

            // Calculate inverse scale to keep handles cubic while growing with the object
            // The handle grows uniformly based on the parent max axis
            handle.transform.localScale = new Vector3(
                currentSize / parentScale.x,
                currentSize / parentScale.y,
                currentSize / parentScale.z
            );

            // Calculate position using cached collider data
            Vector3 newPos = _cachedColCenter;
            float offset;
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

        if (_targetObject.GetComponent<InteractableParent>())
        {             
            ConfirmScaleForParentObject();
        }
        else
        {
            ConfirmScaleForChildObject();
        }

        SaveAsNewOBJ();
        Cleanup();
    }

    private void ConfirmScaleForChildObject()
    {
        // Bake vertices
        MeshFilter mf = _targetObject.GetComponent<MeshFilter>();
        mf.BakeCurrentScale();

        // Reset the transform scale to 1 because the scale is now baked into the mesh vertices
        _targetObject.transform.localScale = Vector3.one;

        // Update the real collider to match new mesh bounds
        _targetCollider.size = mf.sharedMesh.bounds.size;
        _targetCollider.center = mf.sharedMesh.bounds.center;
    }

    private void ConfirmScaleForParentObject()
    {
        // Apply parent scale to childrens
        foreach (Transform child in _targetObject.transform)
        {
            child.localScale = _targetObject.transform.localScale;
        }

        // Reset the parent transform scale to 1 because the scale is now applied to the children
        _targetObject.transform.localScale = Vector3.one;

        foreach (MeshFilter mf in _targetObject.GetComponentsInChildren<MeshFilter>())
        {
            // Bake
            mf.BakeCurrentScale();
            // Reset the transform scale to 1 because the scale is now baked into the mesh vertices
            mf.transform.localScale = Vector3.one;

            var box = mf.GetComponent<BoxCollider>();
            // Update the real collider to match new mesh bounds
            box.size = mf.sharedMesh.bounds.size;
            box.center = mf.sharedMesh.bounds.center;

        }
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
        foreach (GameObject h in _handles) Destroy(h);
        _handles.Clear();

        if (_targetObject != null)
        {
            if (_targetObject.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab)) grab.enabled = true;
            if (_targetCollider != null) _targetCollider.enabled = true;
        }

        _targetObject = null;
    }

    private void SaveAsNewOBJ()
    {
        // Create a new model folder to contain the new mesh 
        InteractableParent iParent = _targetObject.GetComponentInParent<InteractableParent>();

        string ogFileName = Path.GetFileNameWithoutExtension(iParent.Path);

        int pos = ogFileName.IndexOf('#');
        if (pos != -1) // Object was already a mod 
        {
            // Remove mod id if present
            ogFileName = ogFileName.Substring(0, pos);
        }

        string ogDirPath = Path.GetDirectoryName(iParent.Path);

        int i = 0;
        string newFilePath;
        string modID;
        do
        {
            modID = $"#m{++i}.obj";
            string newFileName = string.Concat(ogFileName, modID);
            newFilePath = Path.Combine(ogDirPath, newFileName); // Same folder but different OBJ file 
        } while (File.Exists(newFilePath));

        // Delete old file 
        DeleteModFile(iParent);

        //Update this parent path with the new OBJFile path
        iParent.Path = newFilePath;
        // Update parent name 
        iParent.gameObject.name = string.Concat(iParent.gameObject.name.RemoveModID(), modID);

        // Save mesh
        OBJExporter.Export(iParent);

        RoomManagementTools.Save(FindAnyObjectByType<RoomBuilderManager>().RoomName);
    }


    public void DeepCleanUp()
    {
        string roomsPath = RoomManagementTools.roomsFolderPath;
        string modelsPath = ImportUtils.ModelsPath;

        List<string> allModifiedModelsPaths = new List<string>();
        List<string> foundModifiedModelsPaths = new List<string>();

        foreach (string modelDir in Directory.GetDirectories(modelsPath))
        {
            foreach (string file in Directory.GetFiles(modelDir, "*.obj"))
            {
                if (file.Contains("#m"))
                {
                    // Normalize file path to ensure exact string matching later
                    string normalizedFilePath = Path.GetFullPath(file).Replace('\\', '/');
                    allModifiedModelsPaths.Add(normalizedFilePath);
                    Debug.Log($"Found modified model file: {normalizedFilePath}");
                }
            }
        }

        foreach (string roomFile in Directory.GetFiles(roomsPath, "*.room"))
        {
            string json = File.ReadAllText(roomFile);
            RoomData roomData = JsonUtility.FromJson<RoomData>(json);

            foreach (ParentData data in roomData.objects)
            {
                if (data.objFilePath.Contains("#m"))
                {
                    // Normalize the JSON reference path to match the file system path format
                    string normalizedRefPath = Path.GetFullPath(data.objFilePath).Replace('\\', '/');
                    foundModifiedModelsPaths.Add(normalizedRefPath);
                    Debug.Log($"Found modified model reference: {normalizedRefPath}, in room: {roomFile}");
                }
            }
        }

        foreach (string deletePath in allModifiedModelsPaths.Except(foundModifiedModelsPaths))
        {
            File.Delete(deletePath);
            Debug.Log($"Deleted unused modified model file: {deletePath}");
        }
    }
}