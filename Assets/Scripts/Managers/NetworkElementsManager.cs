using Unity.Netcode;
using UnityEngine;

public class NetworkElementsManager : NetworkBehaviour
{
    [SerializeField] private GameObject spectatorPrefab;
    [SerializeField] private Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Prevent the VR Host from spawning a spectator for itself
        if (clientId == NetworkManager.ServerClientId) return;

        GameObject spectatorInstance = Instantiate(spectatorPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject networkObject = spectatorInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }
}