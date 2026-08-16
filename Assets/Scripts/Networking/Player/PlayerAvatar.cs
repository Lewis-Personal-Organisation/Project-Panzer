using System.Collections;
using System.Collections.Generic;
using MiniTanks;
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    public int playerIndex { get; private set; }
    public string playerId { get; private set; }
    public string playerName { get; private set; }
    public ulong playerRelayId { get; private set; }
    public int score { get; private set; }
    
    
    /// <summary>
    /// Called on all Clients to set up their Player Avatar
    /// </summary>
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
        }
        else
        {
            gameObject.name += " (Other Player)";
        }
    }
}
