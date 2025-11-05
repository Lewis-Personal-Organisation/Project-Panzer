using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Networking;

public class SingleShotWeapon : VehicleWeaponController
{
    private Queue<NetworkObject> availableShells = new Queue<NetworkObject>();
    private HashSet<NetworkObject> activeShells = new HashSet<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        // Only server creates the pool
        if (NetworkManager.Singleton.IsServer)
        {
            InitializeServerPool();
        }
    }

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
        for (int i = 0; i < initPoolSize; i++)
        {
            WeaponShell shell = Instantiate(weapon.shellPrefab);
            
            // We can't disable NetworkBehaviours, so hide objects
            shell.transform.position = new Vector3(0, -5F, 0);
            shell.isPooled = true;
            
            NetworkObject shellNetObj = shell.networkObject;
            shellNetObj.Spawn(true);
            
            availableShells.Enqueue(shellNetObj);
        }
        Debug.Log($"Server: Created Pool of {initPoolSize} shells for {this.transform.root.gameObject.name}!");
    }
    
    private NetworkObject GetFromPool(Vector3 position, Quaternion rotation, ulong ownerID)
    {
        NetworkObject shellNetObj;
        WeaponShell shell = null;
        
        if (availableShells.Count > 0)
        {
            shellNetObj = availableShells.Dequeue();
            shell = shellNetObj.GetComponent<WeaponShell>();
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
        
        shellNetObj.ChangeOwnership(ownerID);
        activeShells.Add(shellNetObj);
        
        Debug.Log($"Server: Spawned/retrieved Shell and moved it! Active shells: {activeShells.Count}");
        
        return shellNetObj;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ReturnToPoolServerRpc(NetworkObjectReference netObjRef)
    {
        if (!netObjRef.TryGet(out  NetworkObject netObj))
            return;

        if (!activeShells.Remove(netObj))
            return;
        
        Debug.Log("Server: Pooling expired shell");
        netObj.transform.position = new Vector3(0, -5, 0);
        var shell = netObj.GetComponent<WeaponShell>();
        if (shell != null)
        {
            shell.isPooled = true;
        }
        
        // netObj.transform.root.gameObject.SetActive(false);
        availableShells.Enqueue(netObj);
                
        // Notify all clients to deactivate
        DeactivateShellClientRpc(netObj.NetworkObjectId);
    }
    
    protected override void Fire()
    {
        if (reloadTimer > 0)
            return;

        ResetWeapon();
        ShootServerRpc(OwnerClientId, shellSpawnPoint.position, shellSpawnPoint.rotation);
        vehicle.cameraController.Shake(weapon.OnFireShakeParams);
        weaponLeanController.PrepareLean();
    }

    protected override void Reload()
    {
        if (reloadTimer > 0)
            reloadTimer -= Time.deltaTime;
    }

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
        NetworkObject shellNetObj = GetFromPool(position, rotation, ownerID);
        Debug.Log($"Server: Asking clients to move new shell");
        ActivateClientRpc(shellNetObj.NetworkObjectId, position, rotation);
    }
    
    /// <summary>
    /// Finds a spawned object with an ID and attempts to set its position and rotation
    /// </summary>
    [ClientRpc]
    private void ActivateClientRpc(ulong bulletID, Vector3 pos, Quaternion rotation)
    {
        if (NetworkManager.Singleton.IsServer) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bulletID, out var netObj))
            return;
        
        Debug.Log($"Client (All): Moving new shell");
        netObj.transform.SetPositionAndRotation(pos, rotation);
        var shell = netObj.GetComponent<WeaponShell>();
        if (shell != null)
        {
            shell.isPooled = false; // Activate it
            Debug.Log($"Client (All): Disabled pooling of shell");
        }
    }
    
    /// <summary>
    /// Deactivates a spawned shell object for all clients, if found in the list of server spawned objects
    /// </summary>
    [ClientRpc]
    private void DeactivateShellClientRpc(ulong shellID)
    {
        if (NetworkManager.Singleton.IsServer) return;
        
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shellID, out var netObj))
            return;
        
        Debug.Log("Client (All): Deactivating expired shell as requested from Server");
        netObj.transform.position = new Vector3(0, -5, 0);
        var shell = netObj.GetComponent<WeaponShell>();
        if (shell != null)
        {
            shell.isPooled = true;
        }
    }
}