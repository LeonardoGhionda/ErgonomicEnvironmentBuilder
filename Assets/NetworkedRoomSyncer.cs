using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkedRoomSyncer : MonoBehaviour
{
    public static NetworkedRoomSyncer Singleton;
    private const int CHUNK_SIZE = 32000;
    private string _hostSessionName = "";

    private Dictionary<string, MemoryStream> _incomingFiles = new Dictionary<string, MemoryStream>();
    private int _expectedFiles = 0;
    private int _completedFiles = 0;
    private string _pendingJson = "";
    private bool _hasRequestedFiles = false;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += SetupServer;
            NetworkManager.Singleton.OnClientConnectedCallback += SetupClient;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SetupServer;
            NetworkManager.Singleton.OnClientConnectedCallback -= SetupClient;
        }
    }

    public void SetHostSessionName(string sessionName)
    {
        _hostSessionName = sessionName;
    }

    private void SetupServer()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ReqData", HandleDataRequest);
    }

    private void SetupClient(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId && !NetworkManager.Singleton.IsServer)
        {
            if (_hasRequestedFiles) return;
            _hasRequestedFiles = true;

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("RxFile", HandleFileChunk);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("RxJson", HandleJson);

            using FastBufferWriter writer = new FastBufferWriter(0, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ReqData", NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
        }
    }

    private void HandleDataRequest(ulong senderClientId, FastBufferReader messagePayload)
    {
        string jsonPath = RoomManagementTools.RoomFullPath(_hostSessionName);
        string roomJson = RoomManagementTools.LoadJson(jsonPath);
        List<string> filePaths = new List<string>();

        string pattern = @"""objFilePath""\s*:\s*""(.*?)""";
        MatchCollection matches = Regex.Matches(roomJson, pattern);
        string modifiedJson = roomJson;

        foreach (Match m in matches)
        {
            if (m.Groups.Count > 1)
            {
                string originalMatch = m.Groups[1].Value;
                string path = originalMatch.Replace("\\\\", "\\").Replace("/", "\\");
                string fileName = Path.GetFileName(path);
                string clientPath = Path.Combine(Application.persistentDataPath, "DownloadedModels", fileName).Replace("\\", "/");

                modifiedJson = modifiedJson.Replace(originalMatch, clientPath);

                if (!filePaths.Contains(path)) filePaths.Add(path);
            }
        }

        int totalFilesToSend = 0;
        List<string> validPathsToSend = new List<string>();

        foreach (string path in filePaths)
        {
            if (File.Exists(path))
            {
                totalFilesToSend++;
                validPathsToSend.Add(path);
            }

            string mtlPath = path.Substring(0, path.LastIndexOf('.')) + ".mtl";
            if (File.Exists(mtlPath))
            {
                totalFilesToSend++;
                validPathsToSend.Add(mtlPath);
            }
        }

        byte[] jsonData = Encoding.UTF8.GetBytes(modifiedJson);
        using FastBufferWriter jsonWriter = new FastBufferWriter(jsonData.Length + 8, Allocator.Temp);
        jsonWriter.WriteValueSafe(totalFilesToSend);
        jsonWriter.WriteValueSafe(jsonData.Length);
        jsonWriter.WriteBytesSafe(jsonData);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("RxJson", senderClientId, jsonWriter, NetworkDelivery.ReliableFragmentedSequenced);

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

        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, fileData.Length - offset);

            using FastBufferWriter writer = new FastBufferWriter(length + 1024, Allocator.Temp);
            writer.WriteValueSafe(fileName);
            writer.WriteValueSafe(totalChunks);
            writer.WriteValueSafe(i);
            writer.WriteValueSafe(length);

            byte[] chunk = new byte[length];
            Array.Copy(fileData, offset, chunk, 0, length);
            writer.WriteBytesSafe(chunk);

            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("RxFile", clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }

    private void HandleJson(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out _expectedFiles);
        messagePayload.ReadValueSafe(out int length);
        byte[] jsonData = new byte[length];
        messagePayload.ReadBytesSafe(ref jsonData, length);

        _pendingJson = Encoding.UTF8.GetString(jsonData);
        CheckCompletion();
    }

    private void HandleFileChunk(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out string fileName);
        messagePayload.ReadValueSafe(out int totalChunks);
        messagePayload.ReadValueSafe(out int chunkIndex);
        messagePayload.ReadValueSafe(out int length);

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
            DesktopSessionListener.Singleton.CompleteRoomLoad(_pendingJson);
        }
    }
}