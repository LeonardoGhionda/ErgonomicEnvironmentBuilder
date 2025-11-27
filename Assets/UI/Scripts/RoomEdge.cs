using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomEdge : MonoBehaviour, IPointerDownHandler
{
    private RoomDot c1;
    public RoomDot C1 => c1;
    private RoomDot c2;
    public RoomDot C2 => c2;

    private RectTransform rect;
    public RectTransform Rect => rect;

    private RoomBuilderManager rbm;

    public Vector2 Position => rect.anchoredPosition;

    public float Width => rect.rect.width;

    private TextMeshProUGUI sizeText;


    public RoomEdge Init(RoomDot dot1, RoomDot dot2)
    {
        c1 = dot1;
        c2 = dot2;
        rect = GetComponent<RectTransform>();
        sizeText = GetComponentInChildren<TextMeshProUGUI>();
        if (sizeText == null)
        {
            Debug.LogError("RoomEdge: No TextMeshPro component found in children!");
        }

        UpdatePosition(c1.Rect.anchoredPosition, c2.Rect.anchoredPosition);

        return this;
    }

    private void UpdatePosition(Vector2 ap1, Vector2 ap2)
    {
        float width = Vector2.Distance(ap1, ap2);
        // 1. midpoint
        rect.anchoredPosition = (ap1 + ap2) * 0.5f;
        // 2. rotation
        Vector2 dir = ap2 - ap1;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        // 3. size
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rbm = GetComponentInParent<RoomBuilderManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
        UpdateText(); //Can't be called only when position changes due to the scale selector not updating the text otherwise

        //Only update if one of the connected dots is being moved (held by the mouse)
        if (c1.IsHeld || c2.IsHeld)
        {
            UpdatePosition(c1.Rect.anchoredPosition, c2.Rect.anchoredPosition);
        }
    }

    public bool CompareConnections(RoomDot d1, RoomDot d2)
    {
        return (c1 == d1 && c2 == d2) || (c1 == d2 && c2 == d1);
    }

    /// <summary>
    /// Add a new RoomDot in the position where this edge was clicked 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        RoomDot dot = rbm.AddDotOnEdge(this);
    }

    /// <summary>
    /// Shows the lenght of the wall genereted by this edge
    /// </summary>
    private void UpdateText()
    {
        float scale = rbm.Scale;
        float scaleBase = rbm.ScaleBase;
        float worldWidth = Width / scaleBase * scale;
        sizeText.text = worldWidth.ToString("F1") + "m";
    }
}
