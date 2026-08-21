using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public abstract class WeaponAmmoBehaviour : NetworkBehaviour
{
    public NetworkVariable<bool> isPooled = new NetworkVariable<bool>(true,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);                // Is the shell inactive (pooled)
    
    public float baseDamage;
    public Vector3 shellDirection;
    
    // The owner name synced to clients for collisions. Does not accomodate for players joining a session in progress
    public NetworkVariable<NetworkString> ownerName = new NetworkVariable<NetworkString>(new NetworkString(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    
    public abstract void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation);
    
    public abstract void OwnerNetworkUpdate();
    public abstract void OnUpdate();
    
    public abstract void NetworkedFixedUpdate();
    public abstract void OnFixedUpdate();

    /// <summary>
    /// Set the new shell direction (SERVER ONLY)
    /// </summary>
    /// <param name="newDirection"></param>
    [ServerRpc(RequireOwnership = false)]
    public void RotateWithReflectionServerRPC(Vector3 newDirection)
    {
        shellDirection = newDirection;
        
        // To avoid smooth interpolation, use teleport to instantly snap values
        if (IsOwner && TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(transform.position, Quaternion.LookRotation(newDirection), transform.localScale);
        }
        else
        {
            transform.forward = newDirection; // non-owner cosmetic only, will be overwritten by next NetworkTransform snapshot
        }
        
        ReflectClientRpc(newDirection);
    }

    public void RotateWithReflectionLocal(Vector3 newDirection)
    {
        transform.forward = newDirection;
        shellDirection = newDirection;
    }
    
    /// <summary>
    /// Set the new shell direction (CLIENTS ONLY)
    /// </summary>
    /// <param name="direction"></param>
    [ClientRpc]
    private void ReflectClientRpc(Vector3 direction)
    {
        if (IsServer) return; // Server already handled it, return
    
        shellDirection = direction;
        
        // To avoid interpolation, use teleport to instantly snap values
        if (IsOwner && TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(transform.position, Quaternion.LookRotation(direction), transform.localScale);
        }
        
        //transform.forward = direction;
        
        Debug.Log($"Client :: Shell reflection received - Direction: {direction}");
    }
}
