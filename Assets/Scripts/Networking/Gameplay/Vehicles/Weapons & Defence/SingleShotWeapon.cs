using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Networking;

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
            
            NetworkObject shellNetObj = shell.networkObject;
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
        WeaponAmmoBehaviour shell = null;
        
        // Retrieve a shell from Queue, if any are available. Else, spawn a new one
        if (availableShells.Count > 0)
        {
            shellNetObj = availableShells.Dequeue();
            shell = shellNetObj.GetComponent<WeaponAmmoBehaviour>();
            Debug.Log($"Server: Retrieved Shell from Pool! Active shells: {activeShells.Count}");
        }
        else
        {
            // Pool exhausted, create a new one
            shell = Instantiate(weapon.shellPrefab);
            shellNetObj = shell.networkObject;
            shellNetObj.Spawn(true);
            Debug.Log($"Server: Created Shell for Pool! Active shells: {activeShells.Count}");
        }

        // Configure the shell
        shell.Setup(this, position, rotation);
        shell.isPooled = false;
        
        // Set it's ownership and register it
        shellNetObj.ChangeOwnership(ownerID);
        activeShells.Add(shellNetObj);
        
        Debug.Log($"Server: Spawned/retrieved Shell and moved it! Active shells: {activeShells.Count}");
        
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
            ShootServerRpc(OwnerClientId, shellSpawnPoint.position, shellSpawnPoint.rotation);
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
    /// Server retrieves a shell from the pool and configures it, sending updated position and rotation to clients
    /// </summary>
    [ServerRpc]
    private void ShootServerRpc(ulong ownerID, Vector3 position, Quaternion rotation)
    {
        NetworkObject shellNetObj = GetFromOrAddToPool(position, rotation, ownerID);
        audioSource.PlayOneShot(weapon.fireAudio);
        Debug.Log($"Server: Creating Shell");
        Debug.Log($"Server: Playing Fire audio at {this.gameObject.transform.position}");
        ActivateClientRpc(shellNetObj.NetworkObjectId, position, rotation);
    }
    
    /// <summary>
    /// Finds a spawned object with an ID and attempts to set its position and rotation
    /// </summary>
    [ClientRpc]
    private void ActivateClientRpc(ulong bulletID, Vector3 pos, Quaternion rotation)
    {
        if (NetworkManager.Singleton.IsServer) return;  // Don't run this on the server
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bulletID, out var netObj))
            return;
        
        Debug.Log($"Clients: Moving new shell");
        Debug.Log($"Clients: Playing Fire audio at {this.gameObject.transform.position}");
        audioSource.PlayOneShot(weapon.fireAudio);  // Play Gunfire sound
        
        netObj.transform.SetPositionAndRotation(pos, rotation);
        var shell = netObj.GetComponent<WeaponShell>();
        if (shell != null)
        {
            shell.isPooled = false; // Activate it
            Debug.Log($"Clients: Disabled pooling of shell");
        }
    }
}