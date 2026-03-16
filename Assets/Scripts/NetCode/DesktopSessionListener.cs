using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class DesktopSessionListener : MonoBehaviour
{
    [SerializeField] private int inPort = 6987;
    [SerializeField] private int outPort = 6988;

    private UdpClient _udpListener;
    private string _sessionName = "";
    private string _hostIp = "";
    private ushort _hostPort = 0;

    private bool _inviteReceived = false;
    private bool _waitingForRoom = false;
    private bool _roomReceived = false;
    private string _roomJson = "";

    public Action<string> InvitationRecevied;
    public Action<(string, string)> RoomDataReceived;

    private void Start()
    {
        _udpListener = new UdpClient(inPort);
        _ = _udpListener.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint endPoint = new(IPAddress.Any, inPort);
            byte[] receivedBytes = _udpListener.EndReceive(ar, ref endPoint);
            string message = Encoding.UTF8.GetString(receivedBytes);

            if (!_waitingForRoom)
            {
                string[] parts = message.Split('|');
                if (parts.Length == 4 && parts[0] == "VR_INVITE")
                {
                    _sessionName = parts[1];
                    _hostIp = parts[2];
                    _hostPort = ushort.Parse(parts[3]);
                    _inviteReceived = true;
                }
            }
            else
            {
                _roomJson = message;
                _roomReceived = true;
            }

            _ = _udpListener.BeginReceive(ReceiveCallback, null);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void Update()
    {
        if (_inviteReceived)
        {
            _inviteReceived = false;
            HandleInvite();
        }

        if (_roomReceived)
        {
            _roomReceived = false;
            HandleRoomData();
        }
    }

    private void HandleInvite()
    {
        InvitationRecevied?.Invoke(_sessionName);
    }

    private void HandleRoomData()
    {
        string filepath = RoomManagementTools.RoomFullPath(_sessionName);
        RoomDataReceived?.Invoke((filepath, _roomJson));
        ConfigureTransport();
    }

    public void AcceptInvite()
    {
        _waitingForRoom = true;
        SendAcknowledgment();
    }

    private void SendAcknowledgment()
    {
        try
        {
            UdpClient sender = new();
            byte[] data = Encoding.UTF8.GetBytes("HELLO");
            IPEndPoint hostEndPoint = new(IPAddress.Parse(_hostIp), outPort);

            _ = sender.Send(data, data.Length, hostEndPoint);
            sender.Close();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send ACK: {e.Message}");
        }
    }

    private void ConfigureTransport()
    {
        UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(_hostIp, _hostPort);
        enabled = false;
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