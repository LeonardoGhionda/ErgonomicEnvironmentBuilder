using UnityEngine;

public class DeleteRoomDot : MonoBehaviour
{
    private RoomDot targetDot;
    private RectTransform rect;

    private RoomBuilderManager roomBuilderManager;

    //Move the button near the RoomDot 
    public void SetTargetDot(RoomDot roomDot)
    {
        if (roomBuilderManager.DotCount() > 3)
        {
            targetDot = roomDot;
            rect.position = roomDot.Rect.position;

        }
    }

    void Start()
    {
        gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            if (targetDot != null)
            {
                targetDot.Delete();
            }
        });
        rect = GetComponent<RectTransform>();
        roomBuilderManager = FindAnyObjectByType<RoomBuilderManager>();
        Hide();
    }

    public void Hide()
    {
        // Move the button off-screen instead of deactivating it
        rect.anchoredPosition = new Vector2(-10, -10);
    }
}
