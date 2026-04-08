using Dummiesman;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Transformers;


[Serializable]
public struct RoomDotData
{
    public float apx;
    public float apy;
}

[Serializable]
public struct RoomEdgeData
{
    public int id;
    public float apx;
    public float apy;
    public float rotz;
    public float width;
}

[Serializable]
public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public void LoadFrom(Transform transform)
    {
        position = transform.localPosition;
        rotation = transform.localRotation;
        scale = transform.localScale;
    }

    public void ApplyTo(Transform transform)
    {
        transform.SetLocalPositionAndRotation(position, rotation);
        transform.localScale = scale;
    }
}

[Serializable]
public class BoxColliderData
{
    public Vector3 center;
    public Vector3 size;
    public bool isTrigger;

    public void LoadFrom(BoxCollider bc)
    {
        center = bc.center;
        size = bc.size;
        isTrigger = bc.isTrigger;
    }

    public void ApplyTo(BoxCollider bc)
    {
        bc.center = center;
        bc.size = size;
        bc.isTrigger = isTrigger;
    }
}

[Serializable]
public class ChildrenData
{
    public string id; // Unique identifier for the child object
    public string name;
    public TransformData transform;
    public BoxColliderData colliderData;
    public bool gravityEnabled; // Whether gravity is enabled for this child
    public string snapFollowTargetId; // Optional: name of the target for SnapFollow, if applicable
    public bool interactable;
}

[Serializable]
public class ParentData
{
    public string id;
    public string objFilePath;
    public TransformData transform;
    public List<ChildrenData> children;
}

[Serializable]
public class RoomData
{
    //room layout (walls, column)
    public float scale, scaleBase, wallHeigth, wallThickness;
    public Vector2 maxSize;
    public List<RoomDotData> dots = new();
    public List<RoomEdgeData> edges = new();
    //rooms objects 
    public List<ParentData> objects = new();
}

public static class ValidationErrors
{
    internal static string empty = "Room name cannot be empty.";
    internal static string invalid = "Invalid characters. Use only letters, numbers, _ or -.";
    internal static string space = "Room name cannot contain spaces.";
    internal static string inUse = "A room with this name already exists. Click Confirm again to overwrite";
}

/// <summary>
/// Handle saving and creations of rooms
/// </summary>
static public class RoomManagementTools
{

    public static string floorName = "Floor";
    public static string roofName = "Roof";

    public static TagHandle roofTag;

    public static readonly string roomsFolderPath;

    //Runs automatically the first time the class is accessed
    static RoomManagementTools()
    {
        roomsFolderPath = Path.Combine(Application.persistentDataPath, "Rooms Saved");
        if (!Directory.Exists(roomsFolderPath))
            _ = Directory.CreateDirectory(roomsFolderPath);
        
        roofTag = TagHandle.GetExistingTag("Roof");
    }

    static public string RoomFullPath(string roomName) => Path.ChangeExtension(Path.Combine(roomsFolderPath, roomName), "room");

    /// <summary>
    /// Generates a RoomData that contains the minimum required information to save
    /// a room layout 
    /// Used to save the room the first time, when no object are present.
    /// </summary>
    /// <param roomName="rbm"> RoomBuilderManager used to generate the room layot</param>
    /// <returns>Json that contains all the data</returns>
    static public string SaveRoomLayout(RoomBuilderManager rbm)
    {
        //column serialization
        List<RoomDotData> roomDots = new();

        //walls serialization
        List<RoomEdgeData> roomEdges = new();

        //column
        foreach (RoomDot roomDot in rbm.RoomDots)
        {
            RoomDotData roomDotData = new()
            {
                apx = roomDot.Rect.anchoredPosition.x,
                apy = roomDot.Rect.anchoredPosition.y,
            };

            //add to list
            roomDots.Add(roomDotData);
        }

        int cnt = 0;
        //walls
        foreach (RoomEdge roomEdge in rbm.RoomEdges)
        {
            RoomEdgeData edgeData = new()
            {
                id = cnt++,
                apx = roomEdge.Rect.anchoredPosition.x,
                apy = roomEdge.Rect.anchoredPosition.y,
                rotz = roomEdge.Rect.localEulerAngles.z,
                width = roomEdge.Rect.rect.width,
            };

            // add to list
            roomEdges.Add(edgeData);
        }

        //save all element in a serializable struct that will be converted into a json
        RoomData rd = new()
        {
            dots = roomDots,
            edges = roomEdges,
            scale = rbm.Scale,
            scaleBase = rbm.ScaleBase,
            wallHeigth = rbm.WallHeight,
            wallThickness = rbm.WallThickness,
            maxSize = rbm.RoomRect.rect.size
        };

        return ExportToJson(rd);
    }

    /// <summary>
    /// Save element added, edited or removed by the user 
    /// </summary>
    /// <param name="roomName">name of the room (no extension)</param>
    static public void Save(string roomName)
    {
        //get json from room file
        string path = Path.Combine(roomsFolderPath, roomName + ".room");
        string json = LoadJson(path);

        //get old data 
        RoomData roomData = JsonUtility.FromJson<RoomData>(json);

        //clear the user edited object saved to replace them with the new one
        //room elements (walls, ...) don't change 
        roomData.objects.Clear();

        //Add new objects
        GameObject container = GameObject.Find("Objects Container");
        foreach (Transform parent in container.transform)
        {
            if (parent.childCount == 0) continue; //additional check to empty parent
            //save parent
            ParentData objData = new()
            {
                id = parent.GetComponent<Interactable>().ID,
                objFilePath = parent.GetComponent<InteractableParent>().Path,
                transform = new(),
                children = new()
            };
            objData.transform.LoadFrom(parent.transform);

            //save children
            foreach (Transform go in parent.transform)
            {
                InteractableObject interactableGO = go.GetComponent<InteractableObject>();

                ChildrenData data = new()
                {
                    id = interactableGO.ID,
                    name = go.name,
                    transform = new(),
                    colliderData = new(),
                };

                data.transform.LoadFrom(go.transform);
                data.colliderData.LoadFrom(go.GetComponent<BoxCollider>());

                // Sanp Follow
                data.snapFollowTargetId = string.Empty;
                if (go.TryGetComponent<SnapFollow>(out SnapFollow sf))
                {
                    data.snapFollowTargetId = sf.TargetID;
                }

                // Gravity
                if (go.TryGetComponent<Rigidbody>(out Rigidbody rb)) data.gravityEnabled = rb.useGravity;
                else data.gravityEnabled = false;

                data.interactable = !interactableGO.Locked && !interactableGO.Parent.Locked;

                //add to list
                objData.children.Add(data);
            }
            roomData.objects.Add(objData);
        }

        //save file (overwrite)
        try
        {
            json = ExportToJson(roomData);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[RoomDataExporter]: failed to save room: " + ex.ToString());
        }
    }

    /// <summary>
    /// From Room data to Json
    /// </summary>
    static public string ExportToJson(RoomData data)
    {
        return JsonUtility.ToJson(data, true); // pretty print tur = human-readable
    }

    /// <summary>
    /// Read a json from a .room files 
    /// </summary>
    /// <returns>Json</returns>
    static public string LoadJson(string filepath)
    {
        if (!File.Exists(filepath))
        {
            Debug.LogError($"File not found: {filepath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filepath);
            return json;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// ONLY FOR DESKTOP
    /// Crerate room layout and objects
    /// <param roomName="name">file roomName without extension</param>
    /// </summary>
    static public void CreateDTRoom(string name)
    {
        CreateRoom(LoadRoomDataFromName(name));
    }

    /// <summary>
    /// Create room with additional functionality for VR compability
    /// </summary>
    /// <param name="roomName"></param>
    public static void CreateVrEditRoom(string roomName)
    {

        // Create the generic room
        RoomData data = LoadRoomDataFromName(roomName);
        CreateRoom(data);


#if !USE_XR // Flag Check 
        Debug.LogError("VR function called in Desktop Mode");
#endif

        // --- Get Resources ---

        // All object added at runtime are stored here
        GameObject objectsContainer = GameObject.Find("Objects Container");

        // Get the Selection Manager for VR Only to add Callback to selected object
        VRSelectionManager sm = GameObject.FindFirstObjectByType<VRSelectionManager>();
        if (sm == null)
        {
            Debug.LogError("RoomsUtility.CreateRoom: VRSelectionManager not found in scene!");
            return;
        }

        // --- Floor Teleportation ---

        // Get floor
        string groundName = floorName;
        GameObject ground = GameObject.Find(groundName);

        if (ground == null)
        {
            Debug.LogError($"Ground gameObject not found. Don't chance his name, it should be {groundName}");
            return;
        }


        // Setup ground for teleportation
        TeleportationArea tpArea = ground.AddComponent<TeleportationArea>();
        tpArea.teleportationProvider = GameObject.FindFirstObjectByType<TeleportationProvider>();
        tpArea.colliders.Add(ground.GetComponent<Collider>());
        tpArea.interactionLayers = InteractionLayerMask.GetMask("Teleport");


        // --- Vr Object initialization ---
        InteractableParent[] parents = objectsContainer.GetComponentsInChildren<InteractableParent>();
        ParentData[] parentsData = data.objects.ToArray();

        // We need to iterate again on data to initialize gravity 
        foreach (ParentData parentData in parentsData)
        {
            try
            {
                InteractableParent parent = parents.First(p => p.ID == parentData.id);

                InteractableObject[] children = parent.GetComponentsInChildren<InteractableObject>();


                foreach (ChildrenData childrenData in parentData.children)
                {
                    InteractableObject intObject = children.First(c => c.ID == childrenData.id);
                    SetUpVrObject(intObject.transform, sm, childrenData.gravityEnabled, childrenData.interactable);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Error]: {e}");
            }
        }

    }

    public static void CreateTestRoom(string roomName, GameObject mimicPrefab)
    {
        GameObject objectsContainer = GameObject.Find("Objects Container");

        RoomData data = LoadRoomDataFromName(roomName);
        CreateRoom(data);

        string groundName = floorName;
        GameObject ground = GameObject.Find(groundName);

        if (ground == null)
        {
            Debug.LogError($"Ground gameObject not found. Don't chance his name, it should be {groundName}");
            return;
        }

        TeleportationArea tpArea = ground.AddComponent<TeleportationArea>();
        tpArea.teleportationProvider = GameObject.FindFirstObjectByType<TeleportationProvider>();
        tpArea.colliders.Add(ground.GetComponent<Collider>());
        tpArea.interactionLayers = InteractionLayerMask.GetMask("Teleport");

        InteractableParent[] parents = objectsContainer.GetComponentsInChildren<InteractableParent>();
        ParentData[] parentsData = data.objects.ToArray();

        foreach (ParentData parentData in parentsData)
        {
            try
            {
                InteractableParent parent = parents.First(p => p.ID == parentData.id);

                InteractableObject[] children = parent.GetComponentsInChildren<InteractableObject>();

                foreach (ChildrenData childrenData in parentData.children)
                {
                    InteractableObject intObject = children.First(c => c.ID == childrenData.id);
                    SetUpTestObject(intObject.transform, childrenData.gravityEnabled, childrenData.interactable);

                    GameObject mimicInstance = 
                        UnityEngine.Object.Instantiate(mimicPrefab, intObject.transform.position, intObject.transform.rotation);
                    mimicInstance.GetComponent<NetworkObject>().Spawn();
                    mimicInstance.GetComponent<NetworkPrefabMimic>().InitHostSide(intObject);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Error]: {e}");
            }
        }
    }

    public static void CreateSpectatorRoom(string json)
    {
        RoomData data = JsonUtility.FromJson<RoomData>(json);
        CreateRoomStructure(data);

        Transform objectsContainer = GameObject.Find("Objects Container").transform;

        ParentData[] objsData = data.objects.ToArray();
        foreach (ParentData parentData in objsData)
        {
            OBJLoader loader = new();
            GameObject parent = loader.FindMTLAndLoad(parentData.objFilePath);

            parentData.transform.ApplyTo(parent.transform);

            parent.transform.SetParent(objectsContainer);

            ChildrenData[] children = parentData.children.ToArray();

            HashSet<string> validNames = new(children.Select(c => c.name));

            foreach (ChildrenData childData in children)
            {
                Transform child = parent.transform.Find(childData.name);

                childData.transform.ApplyTo(child.transform);

                child.gameObject.AddComponent<InteractableObject>().ID = childData.id;

                if (!validNames.Contains(child.name))
                    GameObject.Destroy(child.gameObject);
            }
        }
    }

    private static void CreateRoomStructure(RoomData data)
    {
        //walls data 
        List<RoomEdgeData> edges = data.edges;

        //column data
        List<RoomDotData> dots = data.dots;

        //get basic room element
        GameObject roomContainer = DependencyProvider.BuildingContainer;
        GameObject baseWallPivot = Resources.Load<GameObject>("Room Builder/Base Wall Pivot");
        GameObject baseColumnPivot = Resources.Load<GameObject>("Room Builder/Base Column Pivot");
        GameObject roof = Resources.Load<GameObject>("Room Builder/Roof");
        GameObject ground = Resources.Load<GameObject>("Room Builder/Ground");

        //create walls
        foreach (RoomEdgeData e in edges)
        {
            GameObject wallPivot = UnityEngine.Object.Instantiate(baseWallPivot);
            Transform wall = wallPivot.transform.GetChild(0);

            // wall name with index to easily find them, if needed.
            // Wall are always in the same order of the edges list, so index like this is correct
            wallPivot.name += $"_{e.id}";
            wall.name = e.id.ToString();

            //All room element are stored in this container
            wallPivot.transform.SetParent(roomContainer.transform, true);
            wallPivot.SetActive(true);

            //set lenght
            float wallLength = e.width / data.scaleBase * data.scale;

            //set position
            wallPivot.transform.position = new Vector3(
                (e.apx - data.maxSize.x / 2) / data.scaleBase * data.scale - wallLength / 2,
                0f,
                (e.apy - data.maxSize.y / 2) / data.scaleBase * data.scale
            );

            //set scale
            wallPivot.transform.localScale = new Vector3(wallLength, data.wallHeigth, data.wallThickness);

            //get the center of the rotation
            Vector3 rotCenter = new(
                wallPivot.transform.position.x + wallLength / 2,
                0.0f,
                wallPivot.transform.position.z
            );

            //rotate
            wallPivot.transform.RotateAround(rotCenter, Vector3.up, -e.rotz);
        }

        //create columns
        foreach (RoomDotData d in dots)
        {
            GameObject column = UnityEngine.Object.Instantiate(baseColumnPivot);

            //All room element are stored in this container
            column.transform.SetParent(roomContainer.transform, true);

            column.SetActive(true);

            //set position
            column.transform.position = new Vector3(
                (d.apx - data.maxSize.x / 2) / data.scaleBase * data.scale,
                0f,
                (d.apy - data.maxSize.y / 2) / data.scaleBase * data.scale
            );

            //set scale 
            column.transform.localScale = new Vector3(data.wallThickness, data.wallHeigth, data.wallThickness);
        }

        //setup ground 
        GameObject groundInstance = UnityEngine.Object.Instantiate(ground);
        groundInstance.name = floorName;
        groundInstance.transform.SetParent(roomContainer.transform, true);

        //setup roof
        GameObject roofInstance = UnityEngine.Object.Instantiate(roof);
        roofInstance.name = roofName;
        roofInstance.transform.SetParent(roomContainer.transform, true);

        //saved wall height
        roofInstance.transform.position = new Vector3(0f, data.wallHeigth, 0f);
    }

    /// <summary>
    /// Common room creation operations for both VR and DT
    /// </summary>
    /// <param name="data"></param>
    static private void CreateRoom(RoomData data)
    {
        CreateRoomStructure(data);

        //all object added at runtime are stored here
        GameObject objectsContainer = GameObject.Find("Objects Container");

        List<(string, SnapFollow)> snapFollowTargets = new();

        //create objects
        ParentData[] objsData = data.objects.ToArray();
        foreach (ParentData parentData in objsData)
        {
            //load obj file
            OBJLoader loader = new();
            GameObject parent = loader.FindMTLAndLoad(parentData.objFilePath);
            // Position setup will be overwritten, camera to null
            RoomEditorState.SetUpModel(parent, parentData.objFilePath, objectsContainer, null);

            // ID 

            ///////
            // Temporary, transition from empty guid allowed 
            string id = parentData.id;
            Guid guid = new(id);
            if (guid == Guid.Empty) guid = Guid.NewGuid();
            //////

            parent.GetComponent<Interactable>().ID = guid.ToString();

            //copy saved transform
            parentData.transform.ApplyTo(parent.transform);

            // Setup children
            ChildrenData[] children = parentData.children.ToArray();

            // Delete Objects previusly deleted but still spwaned becase part of a not deleted parent

            // Cache valid names into a HashSet for O(1) lookup
            HashSet<string> validNames = new(children.Select(c => c.name));

            // Filter and Destroy
            foreach (Transform t in parent.GetComponentsInChildren<Transform>())
            {
                // Skip the parent itself
                if (t == parent.transform) continue;

                // Check against the cached set
                if (!validNames.Contains(t.name))
                {
                    GameObject.Destroy(t.gameObject);
                }
            }

            foreach (ChildrenData childData in children)
            {

                //find correct child by name
                Transform child = parent.transform.Find(childData.name);

                // ID 

                ///////
                // Temporary, transition from empty guid allowed 
                id = childData.id;
                guid = new(id);
                if (guid == Guid.Empty) guid = Guid.NewGuid();
                ///////

                child.GetComponent<Interactable>().ID = guid.ToString();

                //copy saved data
                childData.transform.ApplyTo(child.transform);
                childData.colliderData.ApplyTo(child.GetComponent<BoxCollider>());

                string childID = childData.snapFollowTargetId;
                if (childID != string.Empty && childID != Guid.Empty.ToString())
                {
                    snapFollowTargets.Add((childID, child.AddComponent<SnapFollow>()));
                }
            }
        }

        // SNAP FOLLOW LOGIC 
        foreach ((string, SnapFollow) item in snapFollowTargets)
        {
            try
            {
                string id = item.Item1;
                if (!string.IsNullOrEmpty(id))
                {
                    Transform target = null;
                    if (id.StartsWith("BUILDING")) // Building shell ( floor, cealing or walls )
                    {
                        string name = Path.GetFileName(id);
                        target = GameObject.Find(name).transform;
                    }
                    else // Interactable object
                    {
                        target = objectsContainer.GetComponentsInChildren<Interactable>().First(i => i.ID == id).transform;
                    }

                    item.Item2.GetComponent<SnapFollow>().Init(target);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set up SnapFollow for {item.Item2.gameObject.name}: {ex.Message}");
            }
        }
    }

    public static void SetUpVrObject(Transform obj, VRSelectionManager sm, bool gravityEnabled, bool interactable)
    {
        if (obj.GetComponent<BoxCollider>() == null) _ = obj.AddComponent<BoxCollider>();

        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = gravityEnabled;
        rb.isKinematic = !gravityEnabled;

        XRGrabInteractable xrg = obj.AddComponent<XRGrabInteractable>();
        xrg.throwOnDetach = false;
        xrg.useDynamicAttach = true;
        xrg.selectEntered.AddListener(VRSelectionManager.ReleaseIfLocked);
        xrg.selectEntered.AddListener(args => sm.ChangeSelected(args));
        xrg.retainTransformParent = true;
        xrg.selectMode = InteractableSelectMode.Multiple;

        if(xrg.TryGetComponent(out InteractableObject intObj))
        {
            intObj.Locked = !interactable;
        }

        //Scaling
        XRGeneralGrabTransformer gt = obj.AddComponent<XRGeneralGrabTransformer>();
        gt.allowOneHandedScaling = false;
        gt.allowTwoHandedScaling = true;
        gt.minimumScaleRatio = 0.01f;
        gt.maximumScaleRatio = 20f;
        gt.scaleMultiplier = 0.15f;
    }

    public static void SetUpTestObject(Transform obj, bool gravityEnabled, bool interactable)
    {
        if (!interactable) return;

        if (obj.GetComponent<BoxCollider>() == null) _ = obj.AddComponent<BoxCollider>();

        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = gravityEnabled;
        rb.isKinematic = !gravityEnabled;

        XRGrabInteractable xrg = obj.AddComponent<XRGrabInteractable>();
        xrg.throwOnDetach = !rb.isKinematic;
        xrg.useDynamicAttach = true;
        xrg.retainTransformParent = true;
        xrg.selectMode = InteractableSelectMode.Multiple;
    }

    /// <summary>
    /// Save a room as a .room file with a json format 
    /// </summary>
    /// <param roomName="name"> file roomName without extension</param>
    /// <param roomName="rbm"> Room buoilder manager used to generate the layout</param>
    static public void SaveRoom(string name, RoomBuilderManager rbm, bool overwrite = false)
    {
        //validate new room roomName
        ValidateRoomName(name, overwrite);

        //create a json with room informations
        string json = SaveRoomLayout(rbm);

        //save 
        string filePath = Path.Combine(roomsFolderPath, name + ".room");
        File.WriteAllText(filePath, json);
    }


    /// <summary>
    /// Validate room roomName 
    /// throw exception if roomName not valid 
    /// </summary>
    static private void ValidateRoomName(string name, bool overwrite = false)
    {
        // Empty
        if (string.IsNullOrEmpty(name))
        {
            throw new Exception(ValidationErrors.empty);
        }

        // Invalid characters (only allow letters, numbers, underscore, hyphen)
        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$"))
        {
            throw new Exception(ValidationErrors.invalid);

        }

        // Spaces
        if (name.Contains(" "))
        {
            throw new Exception(ValidationErrors.space);
        }

        // Already exists
        string path = Path.Combine(roomsFolderPath, name + ".room");
        if (!overwrite && File.Exists(path))
        {
            throw new Exception(ValidationErrors.inUse);
        }
    }

    public static void GenerateRoomPreview(string roomName)
    {
        int previewWidth = 1920, previewHeight = 1080;
        string path = Path.Combine(roomsFolderPath, roomName) + ".png";

        // Generate temp camera 
        GameObject tempCamGO = new("TempCaptureCam");
        Camera tempCam = tempCamGO.AddComponent<Camera>();
        
        // Setup temp cam
        Vector3 camParam = GetCameraParameters();
        tempCam.orthographic = true;
        tempCam.transform.SetPositionAndRotation(new(camParam.x, 10f, camParam.y), Quaternion.Euler(90f, 0f, 0f));
        tempCam.orthographicSize = camParam.z;
        tempCam.nearClipPlane = 0.01f;
        tempCam.farClipPlane = 100f;

        // Find and setup Light
        GameObject light =
            GameObject
            .FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(l => l.type == LightType.Directional)
            .gameObject;

        bool currentState = light.activeInHierarchy;
        light.SetActive(true);

        ScreenshotUtility.CaptureCamera(tempCam, previewWidth, previewHeight, path);

        light.SetActive(currentState);
        GameObject.Destroy(tempCam);
        GameObject.Destroy(tempCamGO);
    }

    /// <summary>
    /// Takes 2 walls at random and check if the middle point is inside the room,
    /// repeat several times until a valid point is found
    /// </summary>
    /// <returns>A random position inside the wall or v3 zero if position not found</returns>
    public static Vector3 FindInternalPoint()
    {
        List<GameObject> walls = GameObject.FindGameObjectsWithTag("Wall").ToList();
        int wallMask = LayerMask.GetMask("Wall Layer");

        if (walls == null || walls.Count < 3) return Vector3.zero;

        int _maxAttempts = 50;
        for (int i = 0; i < _maxAttempts; i++)
        {
            // 1. Choose 2 random walls
            GameObject startWall = walls[UnityEngine.Random.Range(0, walls.Count)];
            GameObject targetWall = walls[UnityEngine.Random.Range(0, walls.Count)];
            if (startWall == targetWall)
            {
                i--;
                continue;
            }

            Vector3 startPos = startWall.transform.position;
            Vector3 targetPos = targetWall.transform.position;

            // 2. Direction and distance between them
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);

            // 3. Raycast to check if there are walls in between
            if (Physics.Raycast(startPos + (direction * 0.3f), direction, out RaycastHit hit, distance, wallMask))
            {

                // 4. Midpoint between start e hit
                Vector3 midPoint = (startPos + hit.point) / 2f;

                // 5. Check if point is inside
                if (IsPointInside(midPoint, wallMask))
                {
                    return midPoint;
                }

                // Another try at 20% from start to hit
                Vector3 closePoint = Vector3.Lerp(startPos, hit.point, 0.2f);
                if (IsPointInside(closePoint, wallMask))
                {
                    return closePoint;
                }

            }
        }

        Debug.LogWarning("Failed to find internal point after max attempts.");
        return walls[0].transform.position; // Fallback
    }

    private static RoomData LoadRoomDataFromName(string roomName)
    {
        // Load and read save 
        string filepath = Path.Combine(roomsFolderPath, roomName + ".room");
        string json = LoadJson(filepath);
        return JsonUtility.FromJson<RoomData>(json);
    }

    // Jordan check method to determine if a point is inside a closed shape
    private static bool IsPointInside(Vector3 p, LayerMask layer)
    {
        // Evita la compenetrazione della camera con i muri
        if (Physics.CheckSphere(p, 0.5f, layer)) return false;

        // Usa RaycastNonAlloc per evitare allocazioni
        RaycastHit[] hits = new RaycastHit[32]; // dimensione fissa, adatta se necessario
        int hitCount = Physics.RaycastNonAlloc(p, Vector3.right, hits, 1000f, layer);

        HashSet<Collider> unique = new();
        for (int i = 0; i < hitCount; i++)
            _ = unique.Add(hits[i].collider);

        return unique.Count % 2 != 0;
    }

    public static void CleanupRoom()
    {
        GameObject c = DependencyProvider.BuildingContainer;
        foreach (Transform child in c.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        c = DependencyProvider.ObjectContainer;
        foreach (Transform child in c.transform)
        {
            GameObject.Destroy(child.gameObject);

        }
    }

    /// <summary>
    /// Gets the initialization camera parameters that are centered on the room bounds.
    /// </summary>
    /// <returns>A vector 3 where (x,y) are the camera position (x,z) and z = orthograficSize</returns>
    public static Vector3 GetCameraParameters()
    {
        Transform roomContainer = DependencyProvider.BuildingContainer.transform;

        //Force Unity to update collider bounds immediately
        Physics.SyncTransforms();

        // Get relevant colliders (excluding Ground/Roof)
        BoxCollider[] colliders = roomContainer
            .GetComponentsInChildren<BoxCollider>()
            .Where(c => c.CompareTag("Wall"))
            .ToArray();

        if (colliders.Length == 0) return new Vector3(0, 0, 5f);

        // Calculate total bounds 
        Bounds totalBounds = colliders[0].bounds;
        foreach (BoxCollider c in colliders)
        {
            totalBounds.Encapsulate(c.bounds);
        }

        // Camera calculations
        float padding = 1.2f; // 20% margin
        float screenAspect = Camera.main != null ? Camera.main.aspect : 1.77f;

        // Dimensions
        float roomWidth = totalBounds.size.x;
        float roomHeight = totalBounds.size.z; // Z is height in top-down view
        float roomAspect = roomWidth / roomHeight;

        float orthoSize;

        // Determine limiting dimension
        if (screenAspect >= roomAspect)
        {
            // Screen is wider than room: Fit to Height (Z)
            orthoSize = (roomHeight / 2f) * padding;
        }
        else
        {
            // Screen is narrower than room: Fit to Width (X)
            // Formula: Size = Width / (2 * AspectRatio)
            orthoSize = (roomWidth / (2f * screenAspect)) * padding;
        }

        // Safety clamp to prevent 0 zoom
        orthoSize = Mathf.Max(orthoSize, 2f);

        // Return: X = CenterX, Y = CenterZ, Z = OrthoSize
        return new Vector3(totalBounds.center.x, totalBounds.center.z, orthoSize);
    }
}

