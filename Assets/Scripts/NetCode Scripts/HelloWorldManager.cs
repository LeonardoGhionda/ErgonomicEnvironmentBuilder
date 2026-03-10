using HelloWorld;
using Unity.Netcode;
using UnityEngine;

public class HelloWorldManager : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    public GameObject xrRig;
    public Camera desktopCamera;

    void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        if (m_NetworkManager == null)
        {
            Debug.LogError("HelloWorldManager could not find NetworkManager component");
        }
#if USE_XR
        m_NetworkManager.StartServer();
        Debug.Log("<color=green>Host started successfully</color>");
        GameObject xrRigInstance = Instantiate(xrRig, Vector3.zero, Quaternion.identity);
        xrRigInstance.GetComponent<NetworkObject>().Spawn();
#else
        Instantiate(desktopCamera);
        m_NetworkManager.StartClient();
        Debug.Log("<color=green>Client started successfully</color>");
#endif
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        StatusLabels();
        SubmitNewPosition();
        GUILayout.EndArea();
    }

    void StatusLabels()
    {
        string mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    void SubmitNewPosition()
    {
        if (GUILayout.Button("Move"))
        {
            if (m_NetworkManager.IsServer && !m_NetworkManager.IsClient)
            {
                foreach (ulong uid in m_NetworkManager.ConnectedClientsIds)
                    m_NetworkManager.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().Move();
            }
            else
            {
                NetworkObject playerObject = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
                HelloWorldPlayer player = playerObject.GetComponent<HelloWorldPlayer>();
                player.Move();
            }
        }
    }
}
