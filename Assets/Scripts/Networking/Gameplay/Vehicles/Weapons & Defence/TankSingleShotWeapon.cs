using UnityEngine;
using UnityEngine.Pool;

public class TankSingleShotWeapon : TankWeaponController
{
    private void Start()
    {
        shellPool = new ObjectPool<TankShell>(
            () => Instantiate(tankWeapon.shellPrefab).Setup(this),
            shell =>
            {
                shell.Respawn();
                ResetWeapon();
            },
            shell => shell.Despawn(),
            shell => Destroy(shell.gameObject),
            false,
            initPoolSize,
            tankWeapon.ammoCount);

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
        reloadTimer = tankWeapon.reloadTime;
    }
}