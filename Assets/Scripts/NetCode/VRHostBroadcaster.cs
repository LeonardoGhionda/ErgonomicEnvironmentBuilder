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
        NetworkManager.Singleton.StartHost();
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        string localIp = GetLocalIPAddress();
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
        _outChannel?.Close();
    }

    private void OnApplicationQuit()
    {
        OnDisable();
    }
}