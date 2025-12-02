using Dummiesman;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public struct RoomDotData
{
    public float apx;
    public float apy;
}

[Serializable]
public struct RoomEdgeData
{
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
    public string name;
    public TransformData transform;
    public BoxColliderData colliderData;
}

[Serializable]
public class ObjectData
{
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
    public List<ObjectData> objects = new();
}

public static class ValidationErrors
{
    internal static string empty = "Room name cannot be empty.";
    internal static string invalid = "Invalid characters. Use only letters, numbers, _ or -.";
    internal static string space = "Room name cannot contain spaces.";
    internal static string inUse = "A room with this name already exists. Click Confirm again to overwrite";
}

static public class RoomDataExporter
{

    public static readonly string roomsFolderPath =
        Path.Combine(Application.persistentDataPath, "Rooms Saved");

    //Runs automatically the first time the class is accessed
    static RoomDataExporter()
    {
        if (!Directory.Exists(roomsFolderPath))
            Directory.CreateDirectory(roomsFolderPath);
    }

    /// <summary>
    /// Generates a RoomData that contains the minimum required information to save
    /// a room layout 
    /// </summary>
    /// <param roomName="rbm"> RoomBuilderManager used to generate the room layot</param>
    /// <returns>Json that contains all the data</returns>
    static public string SaveRoomLayout(RoomBuilderManager rbm)
    {
        List<RoomDotData> roomDots = new();
        List<RoomEdgeData> roomEdges = new();

        foreach (var roomDot in rbm.RoomDots)
        {
            RoomDotData roomDotData = new()
            {
                apx = roomDot.Rect.anchoredPosition.x,
                apy = roomDot.Rect.anchoredPosition.y,
            };

            roomDots.Add(roomDotData);
        }
        foreach (var roomEdge in rbm.RoomEdges)
        {
            RoomEdgeData edgeData = new()
            {
                apx = roomEdge.Rect.anchoredPosition.x,
                apy = roomEdge.Rect.anchoredPosition.y,
                rotz = roomEdge.Rect.localEulerAngles.z,
                width = roomEdge.Rect.rect.width,
            };

            roomEdges.Add(edgeData);
        }

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

    static public void Save(string roomName)
    {

        string path = Path.Combine(roomsFolderPath, roomName + ".room");
        //open file 
        string json = LoadJson(path);
        //get old data 
        RoomData roomData = JsonUtility.FromJson<RoomData>(json);
        roomData.objects.Clear(); //avoid duplicate objects
        //Add new data
        //serialize objects
        var container = GameObject.Find("Objects Container");
        if (container == null) Debug.LogError("[RoomDataExporter: SaveRoomObjects()] Cannot find Objects Container");
        foreach (Transform parent in container.transform)
        {
            ObjectData objData = new()
            {
                objFilePath = parent.GetComponent<InteractableParent>().Path,
                transform = new(),
                children = new()
            };
            objData.transform.LoadFrom(parent.transform);

            foreach (Transform go in parent.transform)
            {
                ChildrenData data = new()
                {
                    name = go.name,
                    transform = new(),
                    colliderData = new(),
                };

                data.transform.LoadFrom(go.transform);
                data.colliderData.LoadFrom(go.GetComponent<BoxCollider>());

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
    /// Create a json from a .room files 
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
    /// Translate Roomedges and RoomDot in a 3d Room
    /// Edges -> walls
    /// Dot -> Columns
    /// Size is determinated by the scale value
    /// <param roomName="name">file roomName without extension</param>
    /// </summary>
    /// 
    static public void CreateRoom(string name)
    {
        string filepath = Path.Combine(roomsFolderPath, name + ".room");

        string json = LoadJson(filepath);
        //read json data
        RoomData data = JsonUtility.FromJson<RoomData>(json);
       
        var edges = data.edges;
        var dots = data.dots;

        GameObject roomContainer = GameObject.Find("Room Container");

        GameObject baseWallPivot = Resources.Load<GameObject>("Base Wall Pivot");
        if (baseWallPivot == null)
        {
            Debug.LogError("baseWallPivot prefab is null! It must be in a folder named Resources");
            return;
        }
        GameObject baseColumnPivot = Resources.Load<GameObject>("Base Column Pivot");
        if (baseColumnPivot == null)
        {
            Debug.LogError("baseCoulumnPivot prefab is null! It must be in a folder named Resources");
            return;
        }
        GameObject ground = Resources.Load<GameObject>("Ground");
        if (ground == null)
        {
            Debug.LogError("Ground prefab is null! It must be in a folder named Resources");
            return;
        }
        GameObject roof = Resources.Load<GameObject>("Roof");
        if (roof == null)
        {
            Debug.LogError("Roof prefab is null! It must be in a folder named Resources");
            return;
        }

        //create walls
        foreach (var e in edges)
        {
            var wallPivot = UnityEngine.Object.Instantiate(baseWallPivot);
            wallPivot.transform.SetParent(roomContainer.transform, true);
            wallPivot.SetActive(true);
            float wallLength = e.width / data.scaleBase * data.scale;
            wallPivot.transform.position = new Vector3(
                (e.apx - data.maxSize.x / 2) / data.scaleBase * data.scale - wallLength / 2,
                0f,
                (e.apy - data.maxSize.y / 2) / data.scaleBase * data.scale
            );
            wallPivot.transform.localScale = new Vector3(wallLength, data.wallHeigth, data.wallThickness);
            Vector3 rotCenter = new(
                wallPivot.transform.position.x + wallLength / 2,
                0.0f,
                wallPivot.transform.position.z
            );
            wallPivot.transform.RotateAround(rotCenter, Vector3.up, -e.rotz);
        }

        //create columns
        foreach (var d in dots)
        {
            var column = UnityEngine.Object.Instantiate(baseColumnPivot);
            column.transform.SetParent(roomContainer.transform, true);

            column.SetActive(true);
            column.transform.position = new Vector3(
                (d.apx - data.maxSize.x / 2) / data.scaleBase * data.scale,
                0f,
                (d.apy - data.maxSize.y / 2) / data.scaleBase * data.scale
            );
            column.transform.localScale = new Vector3(data.wallThickness, data.wallHeigth, data.wallThickness);
        }

        //create roof and ground
        var groundInstance = UnityEngine.Object.Instantiate(ground);
        groundInstance.transform.SetParent(roomContainer.transform, true);
        var roofInstance = UnityEngine.Object.Instantiate(roof);
        roofInstance.transform.SetParent(roomContainer.transform, true);
        roofInstance.transform.localScale = new Vector3(
            (data.maxSize.x / data.scaleBase * data.scale / 10),
            1f,
            (data.maxSize.y / data.scaleBase * data.scale / 10)
        );
        roofInstance.transform.position = new Vector3(0f, data.wallHeigth, 0f);

        FreeCameraController fcam = GameObject.FindAnyObjectByType<FreeCameraController>();
        fcam.Roof = roofInstance;

        GameObject objectsContainer = GameObject.Find("Objects Container");
        if (objectsContainer == null)
        {
            Debug.LogError("[RoomDataExporter: CreateRoom]: Cannot find Objects Container");
            return;
        }

        //create objects
        ObjectData[] objsData = data.objects.ToArray();
        foreach (ObjectData parentData in objsData)
        {
            //load obj file and setup children
            OBJLoader loader = new();
            GameObject parent = loader.Load(parentData.objFilePath);
            PlaceModelUiButton.SetUpModel(parent, parentData.objFilePath, objectsContainer);

            parentData.transform.ApplyTo(parent.transform);

            ChildrenData[] children = parentData.children.ToArray();
            foreach (ChildrenData childData in children)
            {
                var child = parent.transform.Find(childData.name);
                childData.transform.ApplyTo(child.transform);
                childData.colliderData.ApplyTo(child.GetComponent<BoxCollider>());
            }
        }
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
}
