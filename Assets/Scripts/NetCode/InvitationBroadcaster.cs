using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class InvitationBroadcaster : MonoBehaviour
{
    [SerializeField] private int broadcastPort = 6987;
    [SerializeField] private float _broadcastFrequence = 5f;

    private UdpClient _outChannel;
    private bool _isBroadcasting = false;
    private float _timer = 0f;
    private string _broadcastMessage;
    private string _roomName;


    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += StopBroadcasting;
    }

    public void StartBroadcasting(string sessionName)
    {
        string localIp = GetLocalIPAddress();
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        transport.SetConnectionData(localIp, transport.ConnectionData.Port, localIp);

        if (!NetworkManager.Singleton.StartHost()) Debug.LogError($"Unable to start host");
        ushort hostPort = transport.ConnectionData.Port;

        _roomName = sessionName;
        _broadcastMessage = "VR_INVITE|" + sessionName + "|" + localIp + "|" + hostPort;

        _outChannel = new UdpClient { EnableBroadcast = true };
        _isBroadcasting = true;
        _timer = _broadcastFrequence;

        FindAnyObjectByType<ModelExchangeManager>().SetHostSessionName(_roomName);
    }

    private void Update()
    {
        if (_isBroadcasting)
        {
            _timer += Time.deltaTime;
            if (_timer >= _broadcastFrequence)
            {
                _timer = 0f;
                byte[] data = Encoding.UTF8.GetBytes(_broadcastMessage);
                IPEndPoint endPoint = new(IPAddress.Broadcast, broadcastPort);
                _outChannel.Send(data, data.Length, endPoint);
            }
        }
    }

    private void StopBroadcasting(ulong clientId)
    {
        // Ignore the host local client connection to prevent shutting down the broadcast instantly
        if (clientId == NetworkManager.ServerClientId) return;

        _isBroadcasting = false;
        this.enabled = false;
        NetworkManager.Singleton.OnClientConnectedCallback -= StopBroadcasting;
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError("IP Resolution Error: " + e.Message);

            // Search all network interfaces for a valid IP if the internet ping fails
            foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    foreach (var ip in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "127.0.0.1";
        }
    }

    private void OnDisable()
    {
        _outChannel?.Close();
    }

    private void OnApplicationQuit()
    {
        OnDisable();
    }
}