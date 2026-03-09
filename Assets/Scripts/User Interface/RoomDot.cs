using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RoomDot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RoomBuilderManager roomBuilderManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform bgRect;
    private RectTransform rect;
    public RectTransform Rect => rect;

    //private RectTransform playerSpawn;

    // References to other RoomDots this one is connected to
    [SerializeField] private RoomDot c1;
    public RoomDot C1
    {
        get { return c1; }
        set { c1 = value; }
    }

    [SerializeField] private RoomDot c2;
    public RoomDot C2
    {
        get { return c2; }
        set { c2 = value; }
    }

    private bool isHeld;
    public bool IsHeld
    {
        get { return isHeld; }
        set { isHeld = value; }
    }

    //timer that make decides if the action is a click or a drag
    private Timer timerClick;
    private bool timerOver = false;

    private DeleteRoomDot deleteRoomDot;

    InputAction snapAction;

    void Awake()
    {
        if (canvas == null)
        {
            Debug.LogError("RoomDot: Canvas not assigned!");
            return;
        }
        if (bgRect == null)
        {
            Debug.LogError("RoomDot: RectTransform not found!");
            return;
        }
        rect = GetComponent<RectTransform>();
        deleteRoomDot = canvas.GetComponentInChildren<DeleteRoomDot>();

        snapAction = roomBuilderManager.GetSnapAction();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        // We only want LMB to be the input
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isHeld = true;

        //timer that checks if its a click ore a longer press
        timerClick = new Timer(_ => timerOver = true, null, 100, Timeout.Infinite);

        //Hide delete button if visible
        deleteRoomDot.Hide();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // We only want LMB to be the input
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isHeld = false;
        //if the timer, started in OnPointerDown is not over
        //the button press is considered as a click, the 
        //delete button shows up

        timerClick.Dispose();
        if (!timerOver)
        {
            if (deleteRoomDot == null)
            {
                Debug.LogError("RoomDot: DeleteRoomDot button not found in scene!");
                return;
            }
            deleteRoomDot.SetTargetDot(this);
        }
        timerOver = false;
    }

    void Update()
    {
        if (!isHeld) return;
        MoveDotToMouse();
    }

    public void MoveDotToMouse(bool validityCheck = true)
    {
        //MOUSE INPUT
        //----------------------------
        Vector2 mouse = Mouse.current.position.ReadValue();
        //convert screen point to local point in Ui rect
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgRect,
            mouse,
            canvas.worldCamera,
            out Vector2 anchored
        );

        //move anchor to bottom left
        anchored.x += bgRect.rect.width / 2;
        anchored.y += bgRect.rect.height / 2;

        //DOT MOVEMENT
        //----------------------------
        //bound checking
        anchored.x = Mathf.Clamp(anchored.x, 0, bgRect.rect.width);
        anchored.y = Mathf.Clamp(anchored.y, 0, bgRect.rect.height);

        if (snapAction == null) snapAction = roomBuilderManager.GetSnapAction();
        //----------SNAP-------------
        if (snapAction.IsPressed())
        {
            anchored += CheckForSnap(anchored);
        }

        //move the dot but save old position to recover if there's invalid movement
        Vector2 oldPos = rect.anchoredPosition;
        rect.anchoredPosition = (Vector3)anchored;

        //VALID MOVE CHECKS 
        //-----------------------------------------------------------------
        if (!validityCheck) return; //avoid control (used when a new node is created)


        foreach (RoomEdge edge in roomBuilderManager.RoomEdges)
        {
            Vector2 a = edge.C1.Rect.anchoredPosition;
            Vector2 b = edge.C2.Rect.anchoredPosition;

            //EDGE INTRERSACTION
            //-----------------------
            if (
                SegmentIntersection(a, b, rect.anchoredPosition, c1.rect.anchoredPosition) ||
                SegmentIntersection(a, b, rect.anchoredPosition, c2.rect.anchoredPosition)
               )
            {
                //restore previous position
                rect.anchoredPosition = oldPos;
                return;
            }
        }

        foreach (RoomDot d in roomBuilderManager.RoomDots)
        {

            foreach (RoomEdge edge in roomBuilderManager.RoomEdges)
            {
                //skip edge that connects this RoomDot
                if (edge.C1 == d || edge.C2 == d) continue;

                Vector2 a = edge.C1.Rect.anchoredPosition;
                Vector2 b = edge.C2.Rect.anchoredPosition;

                //DOT INTERSECT EDGE
                //---------------------------------------------
                Vector2 p = d.Rect.anchoredPosition;

                Vector2 ab = b - a;           // line vector
                Vector2 ap = p - a;           // vector from line start to point
                float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;  // projection factor

                // closest point on line (clamped if you want segment)
                Vector2 closestPoint = a + Mathf.Clamp01(t) * ab;

                // distance from point to line
                float distance = (p - closestPoint).magnitude;

                if (distance <= edge.Rect.rect.height / 2 + rect.rect.height / 2)
                {
                    //restore previous position
                    rect.anchoredPosition = oldPos;
                    return;
                }
            }

            //DOT OVER DOT
            //--------------------------
            if (d == this) continue;
            if (Vector2.Distance(d.rect.anchoredPosition, rect.anchoredPosition) < rect.rect.width)
            {
                //restore previous position
                rect.anchoredPosition = oldPos;
                return;
            }
        }
        //-----------------------------------

    }



    /// <summary>
    /// For each dot check if there are some delta x or y less than a treshold
    /// if more then one select the smallest 
    /// </summary>
    /// <returns>the delta position with that dot</returns>
    private Vector2 CheckForSnap(Vector2 position)
    {
        float threshold = 80f;
        float best = float.MaxValue;
        Vector2 bestDelta = Vector2.zero;

        foreach (RoomDot d in roomBuilderManager.RoomDots)
        {
            if (d == this) continue;

            Vector2 q = d.rect.anchoredPosition;

            // X
            float dx = q.x - position.x;
            if (Mathf.Abs(dx) < threshold && Mathf.Abs(dx) < Mathf.Abs(best))
            {
                best = dx;
                bestDelta.x = dx;
            }

            // Y
            float dy = q.y - position.y;
            if (Mathf.Abs(dy) < threshold && Mathf.Abs(dy) < Mathf.Abs(best))
            {
                best = dy;
                bestDelta.y = dy;
            }
        }

        return bestDelta;
    }

    bool SegmentIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float edgeRelax = 0.01f)
    {
        Vector2 r = b - a;
        Vector2 s = d - c;

        float rxs = r.x * s.y - r.y * s.x;
        float qpxr = (c.x - a.x) * r.y - (c.y - a.y) * r.x;

        // Parallel or collinear
        if (Mathf.Approximately(rxs, 0f))
            return false;

        float t = ((c.x - a.x) * s.y - (c.y - a.y) * s.x) / rxs;
        float u = qpxr / rxs;

        // t in [0,1] and u in [0,1] -> intersection inside both segments
        return t >= 0f + edgeRelax && t <= 1f - edgeRelax && u >= 0f + edgeRelax && u <= 1f - edgeRelax;
    }




    public void ChangeConnections(RoomDot oldDot, RoomDot newDot)
    {
        if (c1 == oldDot)
        {
            c1 = newDot;
        }
        else if (c2 == oldDot)
        {
            c2 = newDot;
        }
        else
        {
            Debug.LogError("RoomDot: ChangeConnections called with oldDot not connected to this dot!");
        }
    }

    public void Delete()
    {
        if (roomBuilderManager.DotCount() > 3)
        {
            c1.ChangeConnections(this, c2);
            c2.ChangeConnections(this, c1);
            roomBuilderManager.RemoveDot(this);
            Destroy(gameObject);
        }
    }
}
