using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public enum HandMenuInput
{
    RIGHT,
    LEFT,
    CONFIRM
}

public class HandMenuManager : MonoBehaviour
{
    [SerializeField] ControllerInputActionManager hand;

    private readonly float _stepAngle = 45f;
    private int _maxEntriesShown = 8;
    private List<HM_Base> _entries;
    public List<HM_Base> Entries => _entries;

    private LinkedList<int> _right, _left;
    private int _selected;

    private int _lMax, _rMax;

    [SerializeField] GameObject entryContainer;
    [SerializeField] float radius;

    private bool _open = false;

    public bool Lock { get; set; }

    private void Awake()
    {
        _entries = new List<HM_Base>();
        _right = new LinkedList<int>();
        _left = new LinkedList<int>();
    }
    
    public void Init()
    {
        Lock = false;
        transform.SetParent(hand.transform);
        transform.localPosition = Vector3.zero;
        Show(_open);
    }

    // --- Entries Movement ---
    private void SetUp()
    {
        // Disable all initially to ensure clean state
        _entries.ForEach(e => {

            e.gameObject.SetActive(false);

            if(e.TryGetComponent<CircularMoveUI>(out var circularMove))
                circularMove.Radius = radius;
        });

        // Clamp max entries to actual count
        _maxEntriesShown = Mathf.Min(_entries.Count, 8);
        _selected = 0;

        // Calculate distribution
        if (_maxEntriesShown % 2 == 0) // EVEN
        {
            _lMax = _maxEntriesShown / 2;
            _rMax = _lMax - 1;
        }
        else // ODD
        {
            _rMax = (_maxEntriesShown - 1) / 2;
            _lMax = _rMax;
        }

        // Initial setup
        RefreshMenuState();
        UpdateEntriesPosition();
    }

    public void ProcessInput(HandMenuInput input)
    {
        if(gameObject.activeInHierarchy == false) return;

        switch (input)
        {
            case HandMenuInput.LEFT:
                MoveMenuEntries(false);
                break;

            case HandMenuInput.RIGHT:
                MoveMenuEntries(true);
                break;

            case HandMenuInput.CONFIRM:
                if (_entries.Count > 0)
                {
                    _entries[_selected].OnClick();
                }
                break;
        }
    }

    int InBound(int index)
    {
        if (index < 0)
            return _entries.Count - Mathf.Abs(index) % _entries.Count; // Added modulo for safety
        else if (index >= _entries.Count)
            return index % _entries.Count;
        else
            return index;
    }

    void MoveMenuEntries(bool right)
    {
        if (_entries.Count <= 1) return; // No movement needed for 0 or 1 item

        // Simply update the selected index
        int step = right ? 1 : -1;
        _selected = InBound(_selected + step);

        // Rebuild the visible lists based on the new selection
        RefreshMenuState();
        UpdateEntriesPosition();
    }

    /// <summary>
    /// Clears and repopulates the Left/Right lists based on the current _selected index.
    /// This handles both large and small item counts correctly.
    /// </summary>
    void RefreshMenuState()
    {
        _right.Clear();
        _left.Clear();

        // 1. Reset visual state of all entries (only strictly necessary if Total > MaxShown)
        // If Total <= MaxShown, we keep them active
        foreach (var entry in _entries)
        {
            // Only deactivate if we have more items than we can show
            if (_entries.Count > _maxEntriesShown)
                entry.gameObject.SetActive(false);
        }

        // 2. Setup Selected
        var selectedEntry = _entries[_selected];
        selectedEntry.gameObject.SetActive(true);

        // 3. Populate Right List
        for (int i = 1; i <= _rMax; i++)
        {
            int idx = InBound(_selected + i);
            _right.AddLast(idx);
            _entries[idx].gameObject.SetActive(true);
        }

        // 4. Populate Left List
        for (int i = 1; i <= _lMax; i++)
        {
            int idx = InBound(_selected - i);
            _left.AddLast(idx);
            _entries[idx].gameObject.SetActive(true);
        }
    }

    void UpdateEntriesPosition()
    {
        // Safe check
        if (_entries.Count == 0) return;

        if (_entries[_selected].TryGetComponent<CircularMoveUI>(out var moveUI)) moveUI.SetAngle(0f);

        int i = -1;
        foreach (var index in _left)
        {
            if (_entries[index].TryGetComponent<CircularMoveUI>(out var item)) item.SetAngle(_stepAngle * i);
            i--;
        }
        i = 1;
        foreach (var index in _right)
        {
            if (_entries[index].TryGetComponent<CircularMoveUI>(out var item)) item.SetAngle(_stepAngle * i);
            i++;
        }

        SortChildrenRenderPosition();
    }

    void SortChildrenRenderPosition()
    {
        // Bring active items to front/order logic
        foreach (var item in _entries.Where(e => e.gameObject.activeInHierarchy))
        {
            item.transform.SetAsFirstSibling();
        }

        // Standard painting order from back to front
        for (int i = _rMax - 1; i >= 0; i--)
        {
            if (i < _left.Count)
            {
                _entries[_left.ElementAt(i)].transform.SetAsLastSibling();
            }

            if (i < _right.Count)
            {
                _entries[_right.ElementAt(i)].transform.SetAsLastSibling();
            }
        }

        if (_entries.Count > 0)
            _entries[_selected].transform.SetAsLastSibling();
    }

    // --- Entries Handling ---
    public void AddMenuEntries(List<HM_Base> entries, HM_Base.Dependencies deps)
    {
        entries.ForEach(e => {
            e.transform.SetParent(transform, false);
            e.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            e.Initialize(deps);
        });

        _entries.AddRange(entries);
        SetUp();
    }

    public void RemoveAllEntries()
    {
        foreach (var e in _entries.ToList())
        {
            RemoveEntry(e);
        }

        _entries.Clear();
        _right.Clear();
        _left.Clear();
        _selected = 0;
    }

    public void RemoveMenuEntries(List<HM_Base> entries)
    {
        _entries.Where(e => entries.Contains(e)).ToList().ForEach(e => {
            RemoveEntry(e);
            _entries.Remove(e);
        });

        SetUp();
    }

    // don't set this public: just send a list of one element
    private void RemoveEntry(HM_Base e)
    {
        e.transform.SetParent(entryContainer.transform);
        e.gameObject.SetActive(false);
        e.OnRemove();
    }

    public void Show(bool visible)
    {
        if (Lock) return;

        _open = visible;
        gameObject.SetActive(_open);
        hand.LocomotionEnabled(!visible);
    }

    public void Toggle()
    {
        Show(!_open);
    }

    public void TurnOff()
    {
        Lock = false; // Overraide lock state

        Show(false);
        transform.SetParent(null, false);
        RemoveAllEntries();
    }
}