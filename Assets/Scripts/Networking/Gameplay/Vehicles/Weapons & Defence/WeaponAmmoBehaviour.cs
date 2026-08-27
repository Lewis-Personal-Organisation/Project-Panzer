using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public struct ShellSpawnData : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;
    public bool DirtyBool;   // increments every shot - guarantees a change even if pos/rot repeat

    public ShellSpawnData(Vector3 position, Quaternion rotation, bool dirtyBool)
    {
        Position = position;
        Rotation = rotation;
        DirtyBool = dirtyBool;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref DirtyBool);
    }
}

public abstract class WeaponAmmoBehaviour : NetworkBehaviour
{
    [SerializeField]
    internal NetworkTransform networkTransform;
    
    public NetworkVariable<ShellSpawnData> spawnData = new NetworkVariable<ShellSpawnData>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<bool> isPooled = new NetworkVariable<bool>(true,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);                // Is the shell inactive (pooled)
    
    public NetworkVariable<Vector3> spawnPosition = new NetworkVariable<Vector3>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<Quaternion> spawnRotation = new NetworkVariable<Quaternion>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public float baseDamage;
    // public Vector3 shellDirection;
    
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
    }
    
    /// <summary>
    /// Set the new shell direction (CLIENTS ONLY)
    /// </summary>
    /// <param name="direction"></param>
    [ClientRpc]
    private void ReflectClientRpc(Vector3 direction)
    {
        if (IsServer) return; // Server already handled it, return
        
        // To avoid interpolation, use teleport to instantly snap values
        if (IsOwner && TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(transform.position, Quaternion.LookRotation(direction), transform.localScale);
        }
        
        Debug.Log($"Client :: Shell reflection received - Direction: {direction}");
    }
}
