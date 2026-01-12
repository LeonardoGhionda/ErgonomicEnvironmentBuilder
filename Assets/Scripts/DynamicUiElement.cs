using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a UI element that can move based on runtime conditions or user interactions.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DynamicUiElement : MonoBehaviour
{
    RectTransform _rect;

    [SerializeField] RectTransform x, y;
    bool xMoved = false, yMoved = false;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    void OnGUI()
    {
        if (x != null)
        {
            if (!xMoved && x.gameObject.activeInHierarchy)
            {
                _rect.anchoredPosition += new Vector2(x.rect.width, 0);
                xMoved = true;
            }
            else if (xMoved && !x.gameObject.activeInHierarchy)
            {
                _rect.anchoredPosition -= new Vector2(x.rect.width, 0);
                xMoved = false;
            }
        }
        if (y != null)
        {
            if (!yMoved && y.gameObject.activeInHierarchy)
            {
                _rect.anchoredPosition += new Vector2(0, y.rect.width);
                yMoved = true;
            }
            else if (yMoved && !y.gameObject.activeInHierarchy)
            {
                _rect.anchoredPosition -= new Vector2(0, y.rect.width);
                yMoved = false;
            }
        }
    }
}
