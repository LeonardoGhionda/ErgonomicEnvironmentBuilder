using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SpectatorNetworkManager : MonoBehaviour
{
    [SerializeField] private int listenPort = 6987;

    private UdpClient _udpListener;
    private string _sessionName = "";
    private string _hostIp = "";
    private ushort _hostPort = 0;
    private bool _inviteReceived = false;
    private bool _inviteAccepted = false;

    public Action<string> InvitationRecevied;
    public Action<(string, string)> RoomDataReceived;

    private void Start()
    {
        try
        {
            _udpListener = new UdpClient(listenPort);
            _udpListener.BeginReceive(ReceiveCallback, null);
        }
        catch (SocketException e)
        {
            Debug.LogWarning("Port collision detected. Destroying duplicate SpectatorNetworkManager component. Error: " + e.Message);
            Destroy(this);
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        if (_inviteAccepted) return;
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
        _inviteAccepted = true;
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

        bool isNetworkClear = false;
        while (!isNetworkClear)
        {
            try
            {
                using UdpClient testClient = new();
                testClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                isNetworkClear = true;
            }
            catch (SocketException) { }

            if (!isNetworkClear) yield return null;
        }

        yield return new WaitForEndOfFrame();
        yield return null;

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