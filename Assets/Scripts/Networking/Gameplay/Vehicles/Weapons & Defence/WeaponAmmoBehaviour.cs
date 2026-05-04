using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[GenerateSerializationForGenericParameter(1)]
public abstract class WeaponAmmoBehaviour : NetworkBehaviour
{
    public NetworkObject networkObject;
    public bool isPooled = true;                // Is the shell inactive (pooled)
    public float baseDamage;
    internal Vector3 shellDirection;
    
    public NetworkVariable<NetworkString> ownerName = new NetworkVariable<NetworkString>(new NetworkString(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    
    public abstract void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation);
    
    public abstract void OnNetworkedUpdate();
    public abstract void OnUpdate();
    
    public abstract void OnNetworkedFixedUpdate();
    public abstract void OnFixedUpdate();

    /// <summary>
    /// Set the new shell direction (SERVER ONLY)
    /// </summary>
    /// <param name="newDirection"></param>
    [ServerRpc(RequireOwnership = false)]
    public void RotateWithReflectionServerRPC(Vector3 newDirection)
    {
        transform.forward = newDirection;
        shellDirection = newDirection;
        ReflectClientRpc(newDirection);
    }
    
    /// <summary>
    /// Set the new shell direction (CLIENTS ONLY)
    /// </summary>
    /// <param name="direction"></param>
    [ClientRpc]
    private void ReflectClientRpc(Vector3 direction)
    {
        if (IsServer) return; // Server already handled it, return
    
        // Update kinematic rigidbody on clients
        transform.forward = direction;
        shellDirection = direction;
        
        Debug.Log($"Client :: Shell reflection received - Direction: {direction}");
    }
}
