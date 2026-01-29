using UnityEngine;
using UnityEngine.UI;

public class HM_Base : MonoBehaviour
{
    public class Dependencies
    {
        public VRSelectionManager selection;
        public StateManager state;
        public RoomBuilderManager rbm;
        public MeasureManager measure;
        public GameObject player;
        public HandMenuManager hand;
    }

    protected Dependencies _deps;

    public void Initialize(Dependencies deps)
    {
        _deps = deps;
        OnInitialized();
    }

    protected virtual void OnInitialized() 
    {
    } 

    public virtual void OnClick()
    {

    }
}

public class HM_Toggle: HM_Base
{
    private Color _selectedColor, _unselectedColor;
    protected bool _state;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (!ColorUtility.TryParseHtmlString("#80A8FF", out _selectedColor) ||
            !ColorUtility.TryParseHtmlString("#3373FF", out _unselectedColor))
        {
            Debug.LogError("Failed parsing");
        }

        _state = false;
        UpdateVisual();
    }

    override public void OnClick()
    {
        base.OnClick();

        _state = !_state;
        UpdateVisual();
    }
    
    private void UpdateVisual()
    {
        Image _image = GetComponent<Image>();
        if (_image != null) _image.color = _state ? _selectedColor : _unselectedColor;
    }

}
