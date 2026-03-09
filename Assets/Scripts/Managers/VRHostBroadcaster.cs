using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class VRHostBroadcaster : MonoBehaviour
{
    [SerializeField] private int broadcastPort = 4444;
    [SerializeField] private string sessionName = "VR Room";

    private UdpClient udpClient;
    private bool isBroadcasting = false;
    private readonly float broadcastInterval = 1f;
    private float timer = 0f;
    private string broadcastMessage;

    public void StartHostingAndBroadcasting()
    {
        NetworkManager.Singleton.StartHost();

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        string localIp = GetLocalIPAddress();
        ushort hostPort = transport.ConnectionData.Port;

        // Create the payload for the desktop app to parse
        broadcastMessage = "VR_INVITE|" + sessionName + "|" + localIp + "|" + hostPort;

        udpClient = new()
        {
            EnableBroadcast = true
        };
        isBroadcasting = true;
    }

    private void Update()
    {
        if (!isBroadcasting) return;

        timer += Time.deltaTime;
        if (timer >= broadcastInterval)
        {
            timer = 0f;
            byte[] data = Encoding.UTF8.GetBytes(broadcastMessage);
            IPEndPoint endPoint = new (IPAddress.Broadcast, broadcastPort);
            udpClient.Send(data, data.Length, endPoint);
        }
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
        udpClient?.Close();
    }
}
