using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class VRHostBroadcaster : MonoBehaviour
{
    [SerializeField] private int outPort = 6987;
    [SerializeField] private int inPort = 6988;

    private UdpClient _inChannel;
    private UdpClient _outChannel;

    private bool _isBroadcasting = false;
    private readonly float _broadcastInterval = 10f;
    private float _timer = 0f;
    private string _broadcastMessage;
    private string _roomName;

    private bool _spectatorFound = false;
    private IPEndPoint _spectatorEndPoint;

    public void StartHostingAndBroadcasting(string sessionName)
    {
        NetworkManager.Singleton.StartHost();
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        string localIp = GetLocalIPAddress();
        ushort hostPort = transport.ConnectionData.Port;

        _roomName = sessionName;

        _broadcastMessage = "VR_INVITE|" + _roomName + "|" + localIp + "|" + hostPort;
        _outChannel = new UdpClient { EnableBroadcast = true };
        _isBroadcasting = true;
        _timer = _broadcastInterval;

        _inChannel = new UdpClient(inPort);
        _inChannel.BeginReceive(OnSpectatorFound, null);
    }

    private void Update()
    {
        if (_isBroadcasting)
        {
            _timer += Time.deltaTime;
            if (_timer >= _broadcastInterval)
            {
                _timer = 0f;
                byte[] data = Encoding.UTF8.GetBytes(_broadcastMessage);
                IPEndPoint endPoint = new (IPAddress.Broadcast, outPort);
                _outChannel.Send(data, data.Length, endPoint);
            }
        }

        if (_spectatorFound)
        {
            _spectatorFound = false;
            SendRoomDataToSpectator();
        }
    }

    private void OnSpectatorFound(IAsyncResult ar)
    {
        _isBroadcasting = false;

        IPEndPoint senderEndPoint = new (IPAddress.Any, inPort);
        _ = _inChannel.EndReceive(ar, ref senderEndPoint);

        _spectatorEndPoint = new IPEndPoint(senderEndPoint.Address, outPort);
        _spectatorFound = true;
    }

    private void SendRoomDataToSpectator()
    {
        string roomJson = RoomManagementTools.LoadJson(RoomManagementTools.RoomFullPath(_roomName));

        byte[] data = Encoding.UTF8.GetBytes(roomJson);
        _outChannel.Send(data, data.Length, _spectatorEndPoint);

        this.enabled = false;
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    private void OnDisable()
    {
        _inChannel?.Close();
        _outChannel?.Close();
    }

    private void OnApplicationQuit()
    {
        OnDisable();
    }
}