using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    [SerializeField] internal NetworkObject networkObject;
    [SerializeField] private Rigidbody rigidbody;
    
    bool m_IsMovementAllowed = false;
    
    public int playerIndex { get; private set; }
    public string playerId { get; private set; }
    public string playerName { get; private set; }
    public ulong playerRelayId { get; private set; }
    public int score { get; private set; }
    
    
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

        Debug.Log($"Set player avatar for player #{playerIndex}: id:'{playerId}' name:'{playerName}' relay:{relayClientId}");
    }
    
    public void AllowMovement()
    {
        m_IsMovementAllowed = true;
    }
}
