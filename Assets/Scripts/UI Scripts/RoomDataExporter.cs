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
        //column serialization
        List<RoomDotData> roomDots = new();
        
        //walls serialization
        List<RoomEdgeData> roomEdges = new();

        //column
        foreach (var roomDot in rbm.RoomDots)
        {
            RoomDotData roomDotData = new()
            {
                apx = roomDot.Rect.anchoredPosition.x,
                apy = roomDot.Rect.anchoredPosition.y,
            };

            //add to list
            roomDots.Add(roomDotData);
        }

        //walls
        foreach (var roomEdge in rbm.RoomEdges)
        {
            RoomEdgeData edgeData = new()
            {
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
        var container = GameObject.Find("Objects Container");
        foreach (Transform parent in container.transform)
        {
            //save parent
            ObjectData objData = new()
            {
                objFilePath = parent.GetComponent<InteractableParent>().Path,
                transform = new(),
                children = new()
            };
            objData.transform.LoadFrom(parent.transform);

            //save children
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
        // Load and read save 
        string filepath = Path.Combine(roomsFolderPath, name + ".room");
        string json = LoadJson(filepath);
        RoomData data = JsonUtility.FromJson<RoomData>(json);
       
        //walls data 
        var edges = data.edges;

        //column data
        var dots = data.dots;

        //get basic room element
        GameObject roomContainer = GameObject.Find("Room Container");
        GameObject baseWallPivot = Resources.Load<GameObject>("Room Builder/Base Wall Pivot");
        GameObject baseColumnPivot = Resources.Load<GameObject>("Room Builder/Base Column Pivot");
        GameObject ground = Resources.Load<GameObject>("Room Builder/Ground");
        GameObject roof = Resources.Load<GameObject>("Room Builder/Roof");


        //create walls
        foreach (var e in edges)
        {
            var wallPivot = UnityEngine.Object.Instantiate(baseWallPivot);
            
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
        foreach (var d in dots)
        {
            var column = UnityEngine.Object.Instantiate(baseColumnPivot);

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
        var groundInstance = UnityEngine.Object.Instantiate(ground);
        groundInstance.transform.SetParent(roomContainer.transform, true);

        //setup roof
        var roofInstance = UnityEngine.Object.Instantiate(roof);
        roofInstance.transform.SetParent(roomContainer.transform, true);

        //saved wall height
        roofInstance.transform.position = new Vector3(0f, data.wallHeigth, 0f);


        //it need the roof to hide it in orthografic view
        FreeCameraController fcam = GameObject.FindAnyObjectByType<FreeCameraController>();
        fcam.Roof = roofInstance;

        //all object added at runtime are stored here
        GameObject objectsContainer = GameObject.Find("Objects Container");

        //create objects
        ObjectData[] objsData = data.objects.ToArray();
        foreach (ObjectData parentData in objsData)
        {
            //load obj file
            OBJLoader loader = new();
            GameObject parent = loader.Load(parentData.objFilePath);
            PlaceModelUiButton.SetUpModel(parent, parentData.objFilePath, objectsContainer);

            //copy saved transform
            parentData.transform.ApplyTo(parent.transform);

            //setup children
            ChildrenData[] children = parentData.children.ToArray();
            foreach (ChildrenData childData in children)
            {
                //find correct child by name
                var child = parent.transform.Find(childData.name);

                //copy saved data
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
}
