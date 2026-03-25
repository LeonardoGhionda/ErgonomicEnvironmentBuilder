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
    [SerializeField] Camera _DTCamera;

    [Header("VR Player Elemets")]
    [SerializeField] GameObject _VRPlayer;
    [SerializeField] Camera _VRCamera;
    [SerializeField] Transform _RightHand;
    [SerializeField] Transform _LeftHand;

    [Header("Containers")]
    [SerializeField] GameObject _UIElements;
    [SerializeField] GameObject _HMEntries;

    // Getters
    static public GameObject VRPlayer => Instance._VRPlayer;
    static public Camera VRCamera => Instance._VRCamera;
    static public Transform RightHand => Instance._RightHand;
    static public Transform LeftHand => Instance._LeftHand;
    static public GameObject UIElements => Instance._UIElements;
    static public GameObject HMEntries => Instance._HMEntries;

    static public Camera CurrentCamera
    {
        get {
#if USE_XR
            return Instance._VRCamera;
#else
            return Instance._DTCamera;
#endif
        }
    } 



}
