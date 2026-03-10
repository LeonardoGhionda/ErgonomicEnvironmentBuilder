using Unity.Netcode;
using UnityEngine;

public class XrManager : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    public GameObject xrOrigin;
    public GameObject playerBody;
    public Camera desktopCamera;


    void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        if (m_NetworkManager == null)
        {
            Debug.LogError("HelloWorldManager could not find NetworkManager component");
        }
#if USE_XR
        m_NetworkManager.StartHost();
        Debug.Log("<color=green>Host started successfully</color>");
#else
        m_NetworkManager.StartClient();
        Debug.Log("<color=green>Client started successfully</color>");
#endif
        //spawn player locally and register is ownership with netcode
        if (m_NetworkManager.IsServer)
        {
            //Instanziate xr player for host locally for the host
            GameObject player = Instantiate(xrOrigin);
            GameObject networkBody = Instantiate(playerBody);

            //spawn the networked body and assign ownership to the local client
            networkBody.GetComponentInChildren<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

            //link the xr camera to the networked body for movement syncing
            Camera xrCamera = player.GetComponentInChildren<Camera>();
            networkBody.GetComponentInChildren<CopyTransform>().target = xrCamera.transform;
        }
        //Instanziate desktop camera for client locally for the client
        if (m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            Camera player = Instantiate(desktopCamera);
        }
    }
}
