using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class SingleShotWeapon : VehicleWeaponController
{
    /// <summary>
    /// Initialises the weapon shell pool if we are on the server
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Only server creates the pool
        if (NetworkManager.Singleton.IsServer)
        {
            InitializeServerPool();
        }
    }

    /// <summary>
    /// Sets up the systems for this weapon
    /// </summary>
    /// <param name="vehicleController"></param>
    public override void Setup(VehicleController vehicleController)
    {
        base.Setup(vehicleController);
        ResetWeapon();
    }
    
    /// <summary>
    /// Create all the network objects for this pool
    /// </summary>
    private void InitializeServerPool()
    {
        string playerName = GameplayNetworkManager.Instance.GetPlayerName((int)GetComponent<NetworkObject>().OwnerClientId);
        
        for (int i = 0; i < initPoolSize; i++)
        {
            WeaponAmmoBehaviour shell = Instantiate(weapon.shellPrefab);
            
            // We can't disable NetworkBehaviours, so hide objects
            shell.transform.position = new Vector3(0, -5F, 0);
            shell.isPooled = true;
            shell.name = $"Shell (Pooled, {playerName})";
            
            NetworkObject shellNetObj = shell.NetworkObject;
            shellNetObj.Spawn(true);
            
            availableShells.Enqueue(shellNetObj);
        }
        Debug.Log($"Server: Created Pool of {initPoolSize} shells for {this.transform.root.gameObject.name}!");
    }
    
    /// <summary>
    /// Returns the shell's NetworkObject retrieved or created from the pool system
    /// </summary>
    private NetworkObject GetFromOrAddToPool(Vector3 position, Quaternion rotation, ulong ownerID)
    {
        NetworkObject shellNetObj;
        WeaponAmmoBehaviour shell;

        // Retrieve a shell from Queue, if any are available. Else, spawn a new one
        if (availableShells.Count > 0)
        {
            shellNetObj = availableShells.Dequeue();
            shell = shellNetObj.GetComponent<WeaponAmmoBehaviour>();
        }
        else
        {
            // Pool exhausted, create a new one
            shell = Instantiate(weapon.shellPrefab);
            shellNetObj = shell.NetworkObject;
            shellNetObj.Spawn(true);
        }
        
        shell.ownerName.Value = new NetworkString(GameplayNetworkManager.Instance.GetPlayerName((int)ownerID));
        shell.isPooled = false;
        
        // NOTE: Adjusting position and then changing ownership does not guarantee correct behaviour due to a bug with NGO.
        // A synchronisation bug occurs with ownership change. To avoid this, the position is set within the client RPC,
        // so we are sure it is moved correctly and timely!
        
        // Set it's ownership
        shellNetObj.ChangeOwnership(ownerID);
        
        // Configure the shell for Server side only, tagging it with the actual firing player's name
        shell.Setup(this, position, rotation);
        
        activeShells.Add(shellNetObj);
        
        return shellNetObj;
    }
    
    /// <summary>
    /// Attempts to fire this weapon for Server and Clients
    /// Also processes leaning of the vehicle and camera shake
    /// </summary>
    protected override void Fire()
    {
        if (reloadTimer > 0)
            return;

        ResetWeapon();

        if (VehicleController.IsNetworked)
        {
            ShootServerRpc(shellSpawnPoint.position, shellSpawnPoint.rotation);
        }
        else
        {
            WeaponAmmoBehaviour shell = Instantiate(weapon.shellPrefab);
            shell.Setup(this, shellSpawnPoint.position, shellSpawnPoint.rotation);
        }
        
        vehicle.cameraController.Shake(weapon.OnFireShakeParams);
        weaponLeanController.PrepareLean();
    }

    /// <summary>
    /// Counts down the reload timer
    /// </summary>
    protected override void Reload()
    {
        if (reloadTimer > 0)
            reloadTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Resets the reload timer
    /// </summary>
    protected override void ResetWeapon()
    {
        reloadTimer = weapon.reloadTime;
    }
    
    /// <summary>
    /// Server retrieves a shell from the pool and configures it, sending updated position and rotation to clients.
    /// The firing client's ID is taken from the RPC's sender rather than a trusted client-supplied parameter.
    /// </summary>
    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        NetworkObject shellNetObj = GetFromOrAddToPool(position, rotation, rpcParams.Receive.SenderClientId);
        audioSource.PlayOneShot(weapon.fireAudio);
        ActivateClientRpc(shellNetObj, position, rotation);
    }
    
    /// <summary>
    /// Finds the spawned shell referenced by the server and syncs its position, rotation, and pooled state to clients
    /// </summary>
    [ClientRpc]
    private void ActivateClientRpc(NetworkObjectReference shellRef, Vector3 pos, Quaternion rotation)
    {
        if (IsServer) return;  // Don't run this on the server
        if (!shellRef.TryGet(out NetworkObject netObj))
            return;
        
        Debug.LogWarning($"[ActivateClientRpc] Playing Audio for other shell! Setting pos rot to {pos}, {rotation}");
        
        audioSource.PlayOneShot(weapon.fireAudio);  // Play Gunfire sound
        
        // Only the new owner is allowed to teleport a ClientNetworkTransform
        if (netObj.IsOwner && netObj.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(pos, rotation, netObj.transform.localScale);
        }
        
        if (netObj.TryGetComponent<WeaponShell>(out var shell))
        {
            Debug.LogWarning($"[ActivateClientRpc] Activating the shell!");
            shell.isPooled = false; // Activate it
        }
    }
}