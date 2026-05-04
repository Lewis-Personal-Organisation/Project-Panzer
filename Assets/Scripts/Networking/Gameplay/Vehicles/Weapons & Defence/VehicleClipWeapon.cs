using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class VehicleClipWeapon : VehicleWeaponController
{
    [SerializeField] private int clipSize;
    [SerializeField] private int shellsInClip;
    [SerializeField] private float shotDelayTime;
    [SerializeField] private float shotDelayTimer;

    //NOT IMPLEMENTED/FINISHED
    public override void Setup(VehicleController vehicleController)
    {
        base.Setup(vehicleController);
        
        // shellPool = new ObjectPool<WeaponShell>(
        //     () => Instantiate(weapon.shellPrefab).Setup(this),
        //     shell =>
        //     {
        //         shell.Respawn();
        //         ResetWeapon();
        //         shellsInClip--;
        //     },
        //     shell => shell.Despawn(),
        //     shell => Destroy(shell.gameObject),
        //     false,
        //     initPoolSize,
        //     weapon.ammoCount);
        
        ResetWeapon();
    }
    
    protected override void Fire()
    {
        if (shellsInClip == 0 )
            return;
        
        if (shotDelayTimer > 0)
            return;

        // shellPool.Get();
        weaponLeanController.PrepareLean();
    }

    protected override void Reload()
    {
        // If not at max shells
        if (shellsInClip != clipSize)
        {
            // If timer done, refill a shell and reset timer. Else, count down
            if (reloadTimer <= 0)
            {
                shellsInClip++;
                reloadTimer = weapon.reloadTime;
            }
            else
            {
                reloadTimer -= Time.deltaTime;
            }
        }

        // Reduce shot delay timer
        if (shotDelayTimer > 0)
        {
            shotDelayTimer -= Time.deltaTime;
        }
    }

    protected override void ResetWeapon()
    {
        reloadTimer = weapon.reloadTime;        // Reset this for balance purposes
        shotDelayTimer = shotDelayTime;
    }
    
    //NOT IMPLEMENTED/FINISHED
    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Quaternion rot)
    {
        // IMPLEMENT
        throw new System.NotImplementedException();
    }
    
    //NOT IMPLEMENTED/FINISHED
    // [ServerRpc(RequireOwnership = false)]
    // public override void ReturnToPoolServerRpc(NetworkObjectReference netObjRef)
    // {
    //     if (!netObjRef.TryGet(out  NetworkObject netObj))
    //         return;
    //
    //     // if (!activeShells.Remove(netObj))
    //     //     return;
    //     
    //     Debug.Log("Server: Pooling expired shell");
    //     netObj.transform.position = new Vector3(0, -5, 0);
    //     var shell = netObj.GetComponent<WeaponShell>();
    //     if (shell != null)
    //     {
    //         shell.isPooled = true;
    //     }
    //     
    //     // availableShells.Enqueue(netObj);
    //             
    //     // Notify all clients to deactivate
    //     // DeactivateShellClientRpc(netObj.NetworkObjectId);
    // }
}
