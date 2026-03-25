using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// --- HM == Hand Menu ---


/// <summary>
/// Base card, to override initialization and onCLick behaviour.
/// (Allow polymorphism of menu entries)
/// </summary>
public class HM_Base : MonoBehaviour
{
    public void Initialize()
    {
        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    public virtual void OnClick()
    {

    }

    public virtual void OnRemove()
    {
    }
}

/// <summary>
/// Same as HM_BASE but with 2 states
/// </summary>
public class HM_Toggle : HM_Base
{
    private Color _selectedColor, _unselectedColor;
    protected bool _state;
    Image _image;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _image = GetComponent<Image>();
        if (_image == null) Debug.LogError($"Can't find component Image");

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

    protected void UpdateVisual()
    {
        _image.color = _state ? _selectedColor : _unselectedColor;
    }

}

/// <summary>
/// Cards used to enter in submenus, 
/// once inside the submenu the same card is used as a close menu card.
/// When submenu close, the same entries that were present before opening are restored
/// </summary>
public class HM_Group : HM_Base
{
    [SerializeField] protected List<HM_Base> _group;
    List<HM_Base> _resetGroup;
    protected bool _isMenuOpen = false;

    Image _imageComp;
    [SerializeField] Sprite closeSprite;
    Sprite _baseSprite;

    TextMeshProUGUI _textComp;
    [SerializeField] string closeText;
    string _baseText;

    HandMenuManager _handMenu;

    private void Awake()
    {
        // --- Image Init ---

        if (!transform.TryGetComponentOnlyInChildren(out _imageComp))
            Debug.LogError("Image component not found");

        _baseSprite = _imageComp.sprite;

        // --- Text Init ---

        if (!transform.TryGetComponentOnlyInChildren(out _textComp))
            Debug.LogError("TMP UGUI component not found");

        _baseText = _textComp.text;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _handMenu = Managers.Get<HandMenuManager>();
    }

    public override void OnClick()
    {
        base.OnClick();
        _isMenuOpen = !_isMenuOpen;
        if (_isMenuOpen)
        {
            // Save previous cards to restore when menu close
            _resetGroup = new(_handMenu.Entries);

            // Change menu entries
            _handMenu.RemoveAllEntries();
            _handMenu.AddMenuEntries(_group.Append(this).ToList());

            // Now This card will become the close menu card

            // Image
            _imageComp.sprite = closeSprite;

            //Text
            _textComp.text = closeText;
        }
        else
        {
            // Restore menu entries
            _handMenu.RemoveAllEntries();
            _handMenu.AddMenuEntries(_resetGroup);

            // Image
            _imageComp.sprite = _baseSprite;

            //Text
            _textComp.text = _baseText;
        }

    }
}
