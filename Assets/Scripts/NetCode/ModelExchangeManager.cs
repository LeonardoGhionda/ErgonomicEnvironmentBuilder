using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Handle the transfer of the room 3D models (.obj)
/// It achive this throug CustomMessagingManager, Unity netcode
/// API to send larger quantity of data trough the already established
/// connection.
/// </summary>
public class ModelExchangeManager : MonoBehaviour
{
    private const int CHUNK_SIZE = 32000; 
    private string _hostSessionName = "";

    private readonly Dictionary<string, MemoryStream> _incomingFiles = new();
    private int _expectedFiles = 0;
    private int _completedFiles = 0;
    private string _pendingJson = "";
    private bool _hasRequestedFiles = false;
    private CustomMessagingManager MessagingManager => NetworkManager.Singleton.CustomMessagingManager;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SetupServer;
        NetworkManager.Singleton.OnClientConnectedCallback += SetupClient;

        Debug.Log("subscribed to OnServerStarted and On CLient Connected Callback");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= SetupServer;
        NetworkManager.Singleton.OnClientConnectedCallback -= SetupClient;
    }

    public void SetHostSessionName(string sessionName)
    {
        _hostSessionName = sessionName;
    }

    private void SetupServer()
    {
        MessagingManager.RegisterNamedMessageHandler("ReqData", HandleDataRequest);
    }

    private void SetupClient(ulong clientId)
    {
        Debug.Log("Setup client started");
        if (clientId == NetworkManager.Singleton.LocalClientId && !NetworkManager.Singleton.IsServer)
        {
            if (_hasRequestedFiles) return;
            _hasRequestedFiles = true;

            MessagingManager.RegisterNamedMessageHandler("RxFile", HandleFileChunk);
            MessagingManager.RegisterNamedMessageHandler("RxJson", HandleJson);

            using FastBufferWriter writer = new (0, Allocator.Temp);
            MessagingManager.SendNamedMessage("ReqData", NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
        }
    }

    private void HandleDataRequest(ulong senderClientId, FastBufferReader messagePayload)
    {
        string jsonPath = RoomManagementTools.RoomFullPath(_hostSessionName);
        string roomJson = RoomManagementTools.LoadJson(jsonPath);
        List<string> filePaths = new();

        // Pattern to find inside the room.json every objFilePath entry
        string pattern = @"""objFilePath""\s*:\s*""(.*?)""";
        MatchCollection matches = Regex.Matches(roomJson, pattern);

        // This will contain a placeholder instead of the
        // initial path, receiver will change that with 
        // its persistent data path
        string modifiedJson = roomJson;

        foreach (Match m in matches)
        {
            if (m.Groups.Count > 1)
            {
                string originalMatch = m.Groups[1].Value;
                string path = originalMatch.Replace("\\\\", "\\").Replace("/", "\\");
                string fileName = Path.GetFileName(path);

                string placeholderPath = "CLIENT_LOCAL_MODELS_DIR/" + fileName;

                modifiedJson = modifiedJson.Replace(originalMatch, placeholderPath);

                if (!filePaths.Contains(path)) filePaths.Add(path);
            }
        }


        int totalFilesToSend = 0;
        List<string> validPathsToSend = new();

        foreach (string path in filePaths)
        {
            if (File.Exists(path))
            {
                totalFilesToSend++;
                validPathsToSend.Add(path);
            }

            string mtlPath = path[..path.LastIndexOf('.')] + ".mtl";
            if (File.Exists(mtlPath))
            {
                totalFilesToSend++;
                validPathsToSend.Add(mtlPath);
            }
        }

        byte[] jsonData = Encoding.UTF8.GetBytes(modifiedJson);
        using FastBufferWriter jsonWriter = new (jsonData.Length + 8, Allocator.Temp);
        jsonWriter.WriteValueSafe(totalFilesToSend);
        jsonWriter.WriteValueSafe(jsonData.Length);
        jsonWriter.WriteBytesSafe(jsonData);

        // Send information about the transmission
        MessagingManager.SendNamedMessage("RxJson", senderClientId, jsonWriter, NetworkDelivery.ReliableFragmentedSequenced);

        foreach (string path in validPathsToSend)
        {
            SendFile(senderClientId, path);
        }
    }

    private void SendFile(ulong clientId, string localPath)
    {
        byte[] fileData = File.ReadAllBytes(localPath);
        string fileName = Path.GetFileName(localPath);
        int totalChunks = Mathf.CeilToInt((float)fileData.Length / CHUNK_SIZE);

        // Divide file in <totalChunks> chuncks of size <CHUNK_SIZE>
        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, fileData.Length - offset);

            using FastBufferWriter writer = new (length + 1024, Allocator.Temp);
            writer.WriteValueSafe(fileName);
            writer.WriteValueSafe(totalChunks);
            writer.WriteValueSafe(i);
            writer.WriteValueSafe(length);

            byte[] chunk = new byte[length];
            Array.Copy(fileData, offset, chunk, 0, length);
            writer.WriteBytesSafe(chunk);

            MessagingManager.SendNamedMessage("RxFile", clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }

    private void HandleJson(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out _expectedFiles);
        messagePayload.ReadValueSafe(out int length);
        byte[] jsonData = new byte[length];
        messagePayload.ReadBytesSafe(ref jsonData, length);

        string rawJson = Encoding.UTF8.GetString(jsonData);

        string clientSaveDir = Path.Combine(Application.persistentDataPath, "DownloadedModels").Replace("\\", "/");
        _pendingJson = rawJson.Replace("CLIENT_LOCAL_MODELS_DIR", clientSaveDir);

        CheckCompletion();
    }

    private void HandleFileChunk(ulong senderClientId, FastBufferReader messagePayload)
    {

        messagePayload.ReadValueSafe(out string fileName);
        messagePayload.ReadValueSafe(out int totalChunks);
        messagePayload.ReadValueSafe(out int chunkIndex);
        messagePayload.ReadValueSafe(out int length);

        Debug.Log($"received {fileName}");

        byte[] chunkData = new byte[length];
        messagePayload.ReadBytesSafe(ref chunkData, length);

        if (!_incomingFiles.ContainsKey(fileName))
        {
            _incomingFiles[fileName] = new MemoryStream();
        }

        _incomingFiles[fileName].Write(chunkData, 0, length);

        if (chunkIndex == totalChunks - 1)
        {
            string saveDirectory = Path.Combine(Application.persistentDataPath, "DownloadedModels");
            if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

            string savePath = Path.Combine(saveDirectory, fileName);
            File.WriteAllBytes(savePath, _incomingFiles[fileName].ToArray());
            _incomingFiles[fileName].Dispose();
            _incomingFiles.Remove(fileName);

            _completedFiles++;
            CheckCompletion();
        }
    }

    private void CheckCompletion()
    {
        if (!string.IsNullOrEmpty(_pendingJson) && _completedFiles >= _expectedFiles)
        {
            Debug.Log("Models exchange completed");
            FindAnyObjectByType<SpectatorNetworkManager>().CompleteRoomLoad(_pendingJson);

        }
    }
}