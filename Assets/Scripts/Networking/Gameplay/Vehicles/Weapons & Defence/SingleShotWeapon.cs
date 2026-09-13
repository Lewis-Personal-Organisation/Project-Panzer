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

            shell.usedCounter.OnValueChanged += (bool oldVal, bool newVal) =>
            {
                // if Shell has hit tank on all clients, return to server pool
                if (newVal)
                {
                    Debug.Log($"Shell hit on all clients: {newVal}");
                    shell.sourceController.ReturnToPoolServerRpc(shell.NetworkObject);
                }
            };
            
            // We can't disable NetworkBehaviours, so hide objects
            shell.transform.position = new Vector3(0, -5F, 0);
            // shell.isPooled.Value = true;
            shell.name = $"Shell (Pooled, {playerName})";
            
            NetworkObject shellNetObj = shell.NetworkObject;
            shellNetObj.Spawn(true);

            shellLookup.Add(shellNetObj, shell);
            
            pooledShells.Enqueue(shellNetObj);
        }
        Debug.Log($"Server: Created Pool of {initPoolSize} shells for {this.transform.root.gameObject.name}!");
    }
    
    /// <summary>
    /// Returns the shell's NetworkObject retrieved or created from the pool system
    /// </summary>
    private NetworkObject GetFromOrAddToPool(Vector3 position, Quaternion rotation, ulong newOwnerID)
    {
        WeaponAmmoBehaviour shell;
        NetworkObject shellNetObj;
        
        // Get from Pool OR Spawn new
        if (pooledShells.Count > 0)
        {
            shellNetObj = pooledShells.Dequeue();
            shell = shellLookup[shellNetObj];
        }
        else
        {
            shell = Instantiate(weapon.shellPrefab);
            shellNetObj = shell.NetworkObject;
        }

        // Create lookup
        shellLookup.TryAdd(shellNetObj, shell);

        shell.spawnData.Value = new ShellSpawnData(!shell.spawnData.Value.DirtyBool, position, rotation, false, new NetworkString(GameplayNetworkManager.Instance.GetPlayerName((int)newOwnerID)));
        
        // Spawn it for everyone, if not spawned
        if (!shellNetObj.IsSpawned)
            shellNetObj.Spawn(true);
        
        // Called even if already the owner - This is triggers both WeaponShell.OnGainedOwnership()
        // and NetworkTransform's OnOwnershipChanged authority refresh. Authority refresh cant be
        // triggered any other way, and is required for Teleporting() on the new owner
        shellNetObj.ChangeOwnership(newOwnerID);
        
        // Setup locally for server
        shell.Setup(this, position, rotation);
        
        usedShells.Add(shellNetObj);
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
        ActivateGunshotClientRPC(shellNetObj);
    }
    
    /// <summary>
    /// Finds the spawned shell referenced by the server and syncs its position, rotation, and pooled state to clients
    /// </summary>
    [ClientRpc]
    private void ActivateGunshotClientRPC(NetworkObjectReference shellRef)
    {
        if (IsServer) return;  // Don't run this on the server
        if (!shellRef.TryGet(out NetworkObject netObj))
            return;
        
        // Play Gunfire sound
        audioSource.PlayOneShot(weapon.fireAudio);  
    }
}