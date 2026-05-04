using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(VehicleController))]
public abstract class VehicleWeaponController : NetworkedVehicleComponent, IVehicleComponentToggleable
{
    [field: SerializeField] public VehicleWeapon weapon { get; protected set; }
    [field: SerializeField] public AudioSource audioSource { get; protected set; }
    // [field: SerializeField] public ObjectPool<WeaponShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected VehicleWeaponLeanController weaponLeanController;
    [SerializeField] protected int initPoolSize;
    protected float reloadTimer = 0;
    private UnityAction OnSimulate;
    
    protected Queue<NetworkObject> availableShells = new Queue<NetworkObject>();
    protected HashSet<NetworkObject> activeShells = new HashSet<NetworkObject>();


    public virtual void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;
        TryGetLocalComponent(ref weaponLeanController);
        weaponLeanController.Setup(this);
        
        // Add shooting loop
        Enable();
    }

    private void Update()
    {
        OnSimulate?.Invoke();
    }

    public void Enable()
    {
        OnSimulate += () =>
        {
            if (Input.GetMouseButtonDown(0))
            {
                Fire();
                return;
            }
            else
            {
                Reload();
            }

            if (weaponLeanController.shouldLean)
                weaponLeanController.UpdateLeanValues();
        };
    }

    public void Disable()
    {
        OnSimulate = null;
    }
    
    protected abstract void Fire();
    protected abstract void Reload();
    protected abstract void ResetWeapon();

    /// <summary>
    /// Server method for returning a weapon shell to the server pool
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ReturnToPoolServerRpc(NetworkObjectReference netObjRef)
    {
        if (!netObjRef.TryGet(out NetworkObject netObj))
            return;

        if (!activeShells.Remove(netObj))       // If the shell wasnt present in the active list, return
            return;
        
        Debug.Log("Server: Pooling expired shell");
        netObj.transform.position = new Vector3(0, -5, 0);              // Hide (reposition) the shell from gameplay
        
        if (netObj.TryGetComponent<WeaponShell>(out var shell))
        {
            shell.isPooled = true;
        }
        
        availableShells.Enqueue(netObj);
        
        // Notify all clients to deactivate
        DeactivateShellClientRpc(netObj.NetworkObjectId);
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
        
        if (netObj.TryGetComponent<WeaponShell>(out var shell))
        {
            shell.isPooled = true;
        }
    }
}
