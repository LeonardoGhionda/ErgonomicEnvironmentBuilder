using UnityEngine;

public class MoveUiElement : MonoBehaviour
{
    [SerializeField] RectTransform horizontal;
    bool hState;
    [SerializeField] RectTransform vertical;
    bool vState;

    private void Start()
    {
        if (horizontal != null)
            hState = horizontal.gameObject.activeSelf;
        if (vertical != null)
            vState = vertical.gameObject.activeSelf;
    }

    private void Update()
    {
        if(horizontal != null && horizontal.gameObject.activeSelf != hState)
        {
            hState = horizontal.gameObject.activeSelf;
            float width = horizontal.rect.width;
            ((RectTransform)transform).anchoredPosition += new Vector2(hState ? width : -width, 0);
        }

        if(vertical != null && vertical.gameObject.activeSelf != hState)
        {
            vState = vertical.gameObject.activeSelf;
            float height = vertical.rect.height;
            ((RectTransform)transform).anchoredPosition += new Vector2(0, vState ? height : -height);
        }
    }
}
