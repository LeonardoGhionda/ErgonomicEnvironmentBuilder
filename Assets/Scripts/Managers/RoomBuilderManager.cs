using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class RoomBuilderManager : MonoBehaviour
{
    private RectTransform roomRect;
    public RectTransform RoomRect => roomRect;

    private NewRoomUI _view;

    [SerializeField] private NumberSelector scaleSelector;
    [SerializeField] private NumberSelector heightSelector;
    [Tooltip("Pivot (direct parent of a wall) positioned in the corner of a wall")]
    [SerializeField] private GameObject baseWallPivot;
    [Tooltip("Pivot (direct parent of a wall) positioned in the corner of a column")]
    [SerializeField] private GameObject baseColumnPivot;
    [SerializeField] private GameObject ground;
    [SerializeField] private TMP_Text roomName;
    [Tooltip("Ui element used to represent the edge that connects 2 dots")]
    [SerializeField] private GameObject roomEdgeTemplate;
    [Tooltip("Ui element used as button to delete dots")]
    [SerializeField] private DeleteRoomDot deleteRoomDot;

    //Room corner : colums / meet point of 2 walls
    private List<RoomDot> roomDots;
    public RoomDot[] RoomDots => roomDots.ToArray();

    //room edges : walls
    private List<RoomEdge> roomEdges;
    public RoomEdge[] RoomEdges => roomEdges.ToArray();

    InputAction _snapAction;

    //world units that correspond to ScaleBase unit in ui units
    public float Scale
    {
        get { return scaleSelector.Value; }
    }

    //ui units that correspond to scale unit in world units
    public float ScaleBase
    {
        get { return 100f; }
    }

    public float WallHeight
    {
        get { return heightSelector.Value; }
    }

    public float WallThickness
    {
        get { return .5f; }
    }

    public string RoomName
    {
        get
        {
            //clear roomName from textMeshPro added characters
            string roomName = this.roomName.text.Trim();
            roomName = Regex.Replace(roomName, @"\p{C}+", "");
            return roomName;
        }
        set { roomName.text = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init(NewRoomUI view, InputAction snapAction)
    {
        _view = view;
        _snapAction = snapAction;

        roomRect = _view.GetComponent<RectTransform>();
        roomDots = new List<RoomDot>(_view.GetComponentsInChildren<RoomDot>());
        if (roomDots == null)
        {
            Debug.LogError("RoomBuilderManager: No RoomDot components found in children!");
        }
        if (roomDots.Count < 3)
        {
            Debug.LogError("RoomBuilderManager: At least 3 RoomDot components are required to form a room!");
        }
        GenerateEdges();
    }

    /// <summary>
    /// Create edges based on the connections declared by dots 
    /// </summary>
    public void GenerateEdges()
    {
        roomEdges = new List<RoomEdge>();

        foreach (RoomDot dot in roomDots)
        {
            foreach (RoomDot c in new RoomDot[] { dot.C1, dot.C2 })
            {
                if (!EdgeExists(dot, c))
                {
                    CreateEdge(dot, c);
                }
            }
        }
    }

    public void DeleteAllEdges()
    {
        deleteRoomDot.Hide();
        foreach (RoomEdge edge in roomEdges)
        {
            Destroy(edge.gameObject);
        }
        roomEdges.Clear();
    }

    private void CreateEdge(RoomDot d1, RoomDot d2)
    {
        GameObject edgeGO = Instantiate(roomEdgeTemplate, roomEdgeTemplate.transform.parent);
        edgeGO.SetActive(true);
        //name the edge based on the connected dots' numbers
        var m1 = Regex.Match(d1.name, @"\d+");
        var m2 = Regex.Match(d2.name, @"\d+");
        var num1 = m1.Success ? m1.Value : "0";
        var num2 = m2.Success ? m2.Value : "0";
        edgeGO.name = $"Edge_{num1}_{num2}";
        //move the edge to the bottom of the hierarchy so that dots are always on top
        edgeGO.transform.SetSiblingIndex(0);
        RoomEdge edge = edgeGO.GetComponent<RoomEdge>().Init(d1, d2);
        roomEdges.Add(edge);
    }

    /// <summary>
    /// Check if the edge between 2 connected dots exist 
    /// </summary>
    /// <param name="d1">first dot</param>
    /// <param name="d2">second dot</param>
    /// <returns>true: exist<br />false: doesn't exist</returns>
    private bool EdgeExists(RoomDot d1, RoomDot d2)
    {
        foreach (RoomEdge edge in roomEdges)
        {
            if (edge.CompareConnections(d1, d2))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>total number of dots</returns>
    public int DotCount()
    {
        return roomDots.Count;
    }

    /// <summary>
    /// Generate a new dot and connects it to the proper neighboor dots<br />
    /// Then Ui room is restored to visually match the new layout
    /// </summary>
    /// <param name="e">The edge where the new dot will be created</param>
    /// <returns>The new dot</returns>
    public RoomDot AddDotOnEdge(RoomEdge e)
    {
        RoomDot d1 = e.C1;
        RoomDot d2 = e.C2;
        RoomDot newDot = Instantiate(roomDots[0], roomDots[0].transform.parent);
        if (newDot == null) Debug.LogError("newDotNull");
        newDot.C1 = d1;
        newDot.C2 = d2;
        d1.ChangeConnections(d2, newDot);
        d2.ChangeConnections(d1, newDot);
        newDot.MoveDotToMouse(false);
        roomDots.Add(newDot);
        DeleteAllEdges();
        GenerateEdges();

        return newDot;
    }

    /// <summary>
    /// remove the dot and regenerate all edges
    /// </summary>
    /// <param name="dot">dot to be removed</param>
    public void RemoveDot(RoomDot dot)
    {
        if (roomDots.Contains(dot))
        {
            roomDots.Remove(dot);
            DeleteAllEdges();
            GenerateEdges();
        }
    }

    internal InputAction GetSnapAction()
    {
        return _snapAction;
    }
}

