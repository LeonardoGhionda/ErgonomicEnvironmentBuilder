using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CircularMoveUI), typeof(Button))]
public class HandMenuEntry : MonoBehaviour
{
    [SerializeField] private Color unselectedColor;
    [SerializeField] private Color selectedColor;
    [SerializeField] private bool isToggle;
    private bool _state = false;
    
    private void Awake()
    {
        GetComponent<Image>().color = unselectedColor;
    }

    public bool Toggle()
    {
        if (isToggle)
        {
            _state = !_state;

            if (_state)
            {
                GetComponent<Image>().color = selectedColor;
            }
            else
            {
                GetComponent<Image>().color = unselectedColor;
            }
        }
        return _state;
    }

    public void ResetToggleState()
    {
        if (isToggle && _state)
        {
            Toggle();
        }
    }
}
   
