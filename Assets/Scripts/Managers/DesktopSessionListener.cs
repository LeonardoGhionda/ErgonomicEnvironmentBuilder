using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class DesktopSessionListener : MonoBehaviour
{
    [SerializeField] private int listenPort = 4444;

    private UdpClient udpListener;
    private string hostIp = "";
    private ushort hostPort = 0;
    private bool inviteReceived = false;

    private void Start()
    {
        udpListener = new UdpClient(listenPort);
        udpListener.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint endPoint = new (IPAddress.Any, listenPort);
        byte[] receivedBytes = udpListener.EndReceive(ar, ref endPoint);
        string message = Encoding.UTF8.GetString(receivedBytes);

        // Parse the payload to ensure it is a valid invite
        string[] parts = message.Split('|');
        if (parts.Length == 4 && parts[0] == "VR_INVITE")
        {
            hostIp = parts[2];
            hostPort = ushort.Parse(parts[3]);
            inviteReceived = true;
        }

        // Keep listening for future broadcasts
        udpListener.BeginReceive(ReceiveCallback, null);
    }

    private void Update()
    {
        // Handle the UI or connection logic on the main Unity thread
        if (inviteReceived)
        {
            inviteReceived = false;
            HandleInvite();
        }
    }

    private void HandleInvite()
    {
        Debug.Log("VR Session found at " + hostIp);
        // You can link this to your UI to enable a join button
    }

    public void AcceptInvite()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(hostIp, hostPort);
        NetworkManager.Singleton.StartClient();
    }

    private void OnDisable()
    {
        udpListener?.Close();
    }
}