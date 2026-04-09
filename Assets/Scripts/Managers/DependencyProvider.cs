using UnityEngine;

public class DependencyProvider : MonoBehaviour
{
    public static DependencyProvider Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }


    [Header("DT Player Elements")]
    [SerializeField] GameObject _DTPlayer;
    [SerializeField] Camera _DTCamera;

    [Header("VR Player Elemets")]
    [SerializeField] GameObject _VRPlayer;
    [SerializeField] Camera _VRCamera;
    [SerializeField] Transform _RightHand;
    [SerializeField] Transform _LeftHand;

    [Header("Containers")]
    [SerializeField] GameObject _UIElements; // Gizmo, measure lines, handles, ...
    [SerializeField] GameObject _HMEntries; // hand menu entry
    [SerializeField] GameObject _BuildingContainer; // Contains runtime placed building element (walls, column, floor and celing)
    [SerializeField] GameObject _ObjectContainer; // Contains runtime placed or saved object (Interactable)
    [SerializeField] GameObject _MenuRoom; // Contains the starting room for the vr profile

    // Getters
    static public GameObject DTPlayer => Instance._DTPlayer;
    static public GameObject VRPlayer => Instance._VRPlayer;
    static public Camera DTCamera => Instance._DTCamera;
    static public Camera VRCamera => Instance._VRCamera;
    static public Transform RightHand => Instance._RightHand;
    static public Transform LeftHand => Instance._LeftHand;
    static public GameObject UIElements => Instance._UIElements;
    static public GameObject HMEntries => Instance._HMEntries;
    static public GameObject BuildingContainer => Instance._BuildingContainer;
    static public GameObject ObjectContainer => Instance._ObjectContainer;
    static public GameObject MenuRoom => Instance._MenuRoom;

    // Input
    static private AppActions _inputInternal;
    static public AppActions Input
    {
        get
        {
            if (_inputInternal == null)
            {
                _inputInternal = new AppActions();
            }
            return _inputInternal;
        }
    }

    static public GameObject CurrentPlayer
    {
        get
        {
#if USE_XR
            return VRPlayer;
#else
            return DTPlayer;
#endif

        }
    }

    static public Camera CurrentCamera
    {
        get {
#if USE_XR
            return VRCamera;
#else
            return DTCamera;
#endif
        }
    } 



}
