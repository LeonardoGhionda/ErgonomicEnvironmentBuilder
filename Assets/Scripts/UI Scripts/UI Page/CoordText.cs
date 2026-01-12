using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CoordText : MonoBehaviour
{
    TextMeshProUGUI _textComponent;

    void Start()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
        _textComponent.text = "Local";
    }

    public void ChangeCoordinateMode(bool isLocal)
    {
        _textComponent.text = isLocal ? "Local" : "Global";
    }
}