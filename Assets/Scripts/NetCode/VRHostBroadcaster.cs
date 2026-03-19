using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class VRHostBroadcaster : MonoBehaviour
{
    [SerializeField] private int outPort = 6987;

    private UdpClient _outChannel;
    private bool _isBroadcasting = false;
    private float _timer = 0f;
    private string _broadcastMessage;
    private string _roomName;

    public void StartHostingAndBroadcasting(string sessionName)
    {
        string localIp = GetLocalIPAddress();
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        transport.SetConnectionData(localIp, transport.ConnectionData.Port, "0.0.0.0");

        NetworkManager.Singleton.StartHost();
        ushort hostPort = transport.ConnectionData.Port;

        _roomName = sessionName;
        _broadcastMessage = "VR_INVITE|" + _roomName + "|" + localIp + "|" + hostPort;

        _outChannel = new UdpClient { EnableBroadcast = true };
        _isBroadcasting = true;
        _timer = 10f;

        NetworkedRoomSyncer.Singleton.SetHostSessionName(_roomName);
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
                IPEndPoint endPoint = new(IPAddress.Broadcast, outPort);
                _outChannel.Send(data, data.Length, endPoint);
            }
        }
    }

    public void StopBroadcasting()
    {
        _isBroadcasting = false;
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
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