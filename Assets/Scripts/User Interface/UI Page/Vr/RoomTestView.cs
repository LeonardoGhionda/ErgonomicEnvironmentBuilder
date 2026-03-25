using System.Collections.Generic;
using UnityEngine;

public class RoomTestView : MonoBehaviour
{
    private HandMenuManager _handMenu;

    [SerializeField] private List<HM_Base> baseEntries;

    private void Awake()
    {
        // Now this happens in Step 2, before Init() is ever called
        _handMenu = Managers.Get<HandMenuManager>();
    }

    public void Init()
    {
        _handMenu.Init();
        _handMenu.AddMenuEntries(baseEntries);
    }

    public void HandMenuActions(HandMenuInput input)
    {
        _handMenu.ProcessInput(input);
    }
}