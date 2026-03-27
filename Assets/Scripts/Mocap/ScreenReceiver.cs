using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class ScreenReceiver : MonoBehaviour
{
    [Header("Screen Display")]
    [SerializeField] private RawImage displayUI;
    private Texture2D receivedTexture;
    private byte[] readyImageData;
    private bool hasNewImage = false;

    [Header("Network")]
    public int port = 5001;
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private readonly object lockObject = new();

    private byte currentImageFrameId = 255;
    private int expectedChunks = 0;
    private int receivedChunks = 0;
    private readonly byte[][] chunkBuffer = new byte[256][];
    private int totalImageSize = 0;

    void Start()
    {
        receivedTexture = new Texture2D(2, 2);
        displayUI.texture = receivedTexture;

        udpClient = new UdpClient(port);
        endPoint = new IPEndPoint(IPAddress.Any, port);
        udpClient.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            byte[] data = udpClient.EndReceive(ar, ref endPoint);

            // Minimum length is 3 bytes for FrameID, TotalChunks, and ChunkIndex
            if (data != null && data.Length > 3)
            {
                lock (lockObject)
                {
                    ProcessImageChunk(data);
                }
            }

            udpClient.BeginReceive(ReceiveCallback, null);
        }
        catch (ObjectDisposedException) { }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    private void ProcessImageChunk(byte[] data)
    {
        byte frameId = data[0];
        byte total = data[1];
        byte chunkIndex = data[2];

        // Reset buffer for new frame
        if (frameId != currentImageFrameId)
        {
            currentImageFrameId = frameId;
            expectedChunks = total;
            receivedChunks = 0;
            totalImageSize = 0;
            Array.Clear(chunkBuffer, 0, chunkBuffer.Length);
        }

        // Store chunk payload
        if (chunkBuffer[chunkIndex] == null)
        {
            int payloadLength = data.Length - 3;
            chunkBuffer[chunkIndex] = new byte[payloadLength];
            Buffer.BlockCopy(data, 3, chunkBuffer[chunkIndex], 0, payloadLength);

            receivedChunks++;
            totalImageSize += payloadLength;

            if (receivedChunks == expectedChunks)
            {
                AssembleCompleteImage();
            }
        }
    }

    private void AssembleCompleteImage()
    {
        readyImageData = new byte[totalImageSize];
        int currentOffset = 0;

        for (int i = 0; i < expectedChunks; i++)
        {
            if (chunkBuffer[i] == null) return;

            Buffer.BlockCopy(chunkBuffer[i], 0, readyImageData, currentOffset, chunkBuffer[i].Length);
            currentOffset += chunkBuffer[i].Length;
        }

        hasNewImage = true;
    }

    void LateUpdate()
    {
        byte[] imageToLoad = null;

        lock (lockObject)
        {
            if (hasNewImage)
            {
                imageToLoad = readyImageData;
                hasNewImage = false;
            }
        }

        if (imageToLoad != null)
        {
            receivedTexture.LoadImage(imageToLoad);
        }
    }

    private void OnDestroy()
    {
        udpClient?.Close();
        if (receivedTexture != null) Destroy(receivedTexture);
    }
}