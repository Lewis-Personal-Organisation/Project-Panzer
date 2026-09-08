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
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected VehicleWeaponLeanController weaponLeanController;
    [SerializeField] protected int initPoolSize;
    protected float reloadTimer = 0;
    private UnityAction OnSimulate;
    
    protected Queue<NetworkObject> pooledShells = new Queue<NetworkObject>();
    protected HashSet<NetworkObject> usedShells = new HashSet<NetworkObject>();
    protected Dictionary<NetworkObject, WeaponAmmoBehaviour> shellLookup = new Dictionary<NetworkObject, WeaponAmmoBehaviour>(0);


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
            if (vehicle.inputManager.lmbPressed)
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
        
        // If the shell wasn't present in the active list, return
        // Can occur if the shell is already returned on another client
        if (!usedShells.Remove(netObj))       
            return;

        WeaponAmmoBehaviour shell = shellLookup[netObj];
        
        netObj.transform.position = new Vector3(0, -5, 0);              // Hide (reposition) the shell from gameplay
        shell.usedCounter.Value = 0;
        
        shell.spawnData.Value = new ShellSpawnData(
            !shell.spawnData.Value.DirtyBool,
            new Vector3(0, -5, 0),
            Quaternion.identity,
            true,
            new NetworkString(GameplayNetworkManager.Instance.GetPlayerName((int)NetworkManager.ServerClientId)));
        
        // Change ownership back to server for when it needs to respawn and reposition a shell. Not required for server-fired shots
        if (netObj.OwnerClientId != NetworkManager.ServerClientId)
        {
            netObj.ChangeOwnership(NetworkManager.ServerClientId);
        }
        
        pooledShells.Enqueue(netObj);
    }
}
