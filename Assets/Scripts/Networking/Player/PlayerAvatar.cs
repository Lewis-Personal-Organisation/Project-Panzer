using System.Collections;
using System.Collections.Generic;
using MiniTanks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAvatar : NetworkBehaviour
{
    [FormerlySerializedAs("playerIndex")]
    public int index;
    [FormerlySerializedAs("playerId")]
    public string id;
    [FormerlySerializedAs("playerName")]
    public new string name;
    [FormerlySerializedAs("playerRelayId")]
    public ulong relayId;
    public int score;
    
    
    /// <summary>
    /// Called on all Clients to set up their Player Avatar
    /// </summary>
    [ClientRpc]
    public void SetPlayerAvatarClientRpc(int playerIndex, string playerId, string playerName, ulong relayClientId)
    {
        this.index = playerIndex;
        this.id = playerId;
        this.name = playerName;
        this.relayId = relayClientId;

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
