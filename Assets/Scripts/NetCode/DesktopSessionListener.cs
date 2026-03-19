using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class DesktopSessionListener : MonoBehaviour
{
    [SerializeField] private int listenPort = 6987;

    private UdpClient _udpListener;
    private string _sessionName = "";
    private string _hostIp = "";
    private ushort _hostPort = 0;
    private bool _inviteReceived = false;

    public Action<string> InvitationRecevied;
    public Action<(string, string)> RoomDataReceived;

    public static DesktopSessionListener Singleton;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
    }

    private void Start()
    {
        _udpListener = new UdpClient(listenPort);
        _udpListener.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint endPoint = new(IPAddress.Any, listenPort);
            byte[] receivedBytes = _udpListener.EndReceive(ar, ref endPoint);
            string message = Encoding.UTF8.GetString(receivedBytes);

            string[] parts = message.Split('|');
            if (parts.Length == 4 && parts[0] == "VR_INVITE")
            {
                _sessionName = parts[1];
                _hostIp = parts[2];
                _hostPort = ushort.Parse(parts[3]);
                _inviteReceived = true;
            }

            _udpListener.BeginReceive(ReceiveCallback, null);
        }
        catch (ObjectDisposedException) { }
        catch (Exception e)
        {
            Debug.LogError("Receive error: " + e.Message);
        }
    }

    private void Update()
    {
        if (_inviteReceived)
        {
            _inviteReceived = false;
            InvitationRecevied?.Invoke(_sessionName);
        }
    }

    public void AcceptInvite()
    {
        ConfigureTransportAndConnect();
    }

    private void ConfigureTransportAndConnect()
    {
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(_hostIp, _hostPort);
        NetworkManager.Singleton.StartClient();
    }

    public void CompleteRoomLoad(string jsonContent)
    {
        StartCoroutine(DisconnectAndLoadSequence(jsonContent));
    }

    private IEnumerator DisconnectAndLoadSequence(string jsonContent)
    {
        NetworkManager.Singleton.Shutdown();

        while (NetworkManager.Singleton.ShutdownInProgress || NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        string filepath = Path.Combine(Application.persistentDataPath, "tempRoom.json");
        File.WriteAllText(filepath, jsonContent);

        RoomDataReceived?.Invoke((filepath, jsonContent));
    }

    private void OnDisable()
    {
        _udpListener?.Close();
    }

    private void OnApplicationQuit()
    {
        OnDisable();
    }
}