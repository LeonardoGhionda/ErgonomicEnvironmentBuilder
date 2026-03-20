using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;

public class InvitationBroadcaster : MonoBehaviour
{
    [SerializeField] private int broadcastPort = 6987;

    private UdpClient _outChannel;
    private bool _isBroadcasting = false;
    private float _timer = 0f;
    private string _broadcastMessage;
    private string _roomName;

    public void StartBroadcasting(string sessionName)
    {
        string localIp = GetLocalIPAddress();
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        // Accept incoming connections from any network interface
        transport.SetConnectionData(localIp, transport.ConnectionData.Port, "0.0.0.0");

        NetworkManager.Singleton.StartHost();
        ushort hostPort = transport.ConnectionData.Port;

        _roomName = sessionName;
        _broadcastMessage = "VR_INVITE|" + sessionName + "|" + localIp + "|" + hostPort;

        _outChannel = new UdpClient { EnableBroadcast = true };
        _isBroadcasting = true;
        _timer = 10f;

        FindAnyObjectByType<ModelExchangeManager>().SetHostSessionName(_roomName);
    }

    private void Update()
    {
        if (_isBroadcasting)
        {
            _timer += Time.deltaTime;
            if (_timer >= 10f)
            {
                _timer = 0f;
                byte[] data = Encoding.UTF8.GetBytes(_broadcastMessage);
                IPEndPoint endPoint = new(IPAddress.Broadcast, broadcastPort);
                _outChannel.Send(data, data.Length, endPoint);
            }
        }
    }

    public void StopBroadcasting()
    {
        _isBroadcasting = false;
    }

    /// <summary>
    /// Opens a dummy UDP socket and pretends to connect to Google's Public DNS (8.8.8.8). 
    /// The operating system's routing table is forced to reveal which local network interface actually has internet access, 
    /// guaranteeing we capture the correct LAN IP address.
    /// 
    /// Dns.GetHostEntry often fail because they grab the IP addresses of 
    /// VirtualBox, VMware, or VPN adapters instead of the actual Wi-Fi or Ethernet card
    /// </summary>
    private string GetLocalIPAddress()
    {
        try
        {
            using Socket socket = new (AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError("IP Resolution Error: " + e.Message);
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