using System.Collections;
using System.Collections.Generic;
using MiniTanks;
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    [SerializeField] internal NetworkObject networkObject;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] internal VehicleController vehicleController;
    [SerializeField] internal CameraController cameraController;
    
    bool m_IsMovementAllowed = false;
    
    public int playerIndex { get; private set; }
    public string playerId { get; private set; }
    public string playerName { get; private set; }
    public ulong playerRelayId { get; private set; }
    public int score { get; private set; }
    
    
    /// <summary>
    /// Called on all Clients to set up their Player Avatar
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="playerId"></param>
    /// <param name="playerName"></param>
    /// <param name="relayClientId"></param>
    [ClientRpc]
    public void SetPlayerAvatarClientRpc(int playerIndex, string playerId, string playerName, ulong relayClientId)
    {
        this.playerIndex = playerIndex;
        this.playerId = playerId;
        this.playerName = playerName;
        this.playerRelayId = relayClientId;

        // Sanitize the player name to ensure it's not profane.
        // this.playerName = ProfanityManager.SanitizePlayerName(this.playerName);

        GameplayNetworkManager.Instance?.AddPlayerAvatar(this, IsOwner);

        if (IsOwner)
        {
            GameplayNetworkManager.Instance?.SetLocalAvatar(this);
            // GameplayNetworkManager.Instance?.SpawnPlayerCamera();
        }
        else
        {
            this.gameObject.name += " (Other Player)";
        }
        
        // Debug.Log($"Set player avatar for player #{playerIndex}: id:'{playerId}' name:'{playerName}' relay:{relayClientId}");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer && IsOwner) //Only send an RPC to the server from the client that owns the NetworkObject of this NetworkBehaviour instance
        {
            Debug.Log("Spawned Player Avatar on Client. Sending message to server");
            ServerOnlyRpc(0, NetworkObjectId);
        }
    }
    
    [Rpc(SendTo.Server)]
    void ServerOnlyRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
    }
}
