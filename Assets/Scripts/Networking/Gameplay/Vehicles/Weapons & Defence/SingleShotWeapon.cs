using UnityEngine;
using UnityEngine.Pool;

public class SingleShotWeapon : VehicleWeaponController
{
    private void Start()
    {
        shellPool = new ObjectPool<TankShell>(
            () => Instantiate(vehicleWeapon.shellPrefab).Setup(this),
            shell =>
            {
                shell.Respawn();
                ResetWeapon();
            },
            shell => shell.Despawn(),
            shell => Destroy(shell.gameObject),
            false,
            initPoolSize,
            vehicleWeapon.ammoCount);

        ResetWeapon();
    }
    
    protected override void Fire()
    {
        if (reloadTimer > 0)
            return;

        shellPool.Get();
        PrepareLean();
    }

    protected override void Reload()
    {
        if (reloadTimer > 0)
            reloadTimer -= Time.deltaTime;
    }

    protected override void ResetWeapon()
    {
        reloadTimer = vehicleWeapon.reloadTime;
    }
}