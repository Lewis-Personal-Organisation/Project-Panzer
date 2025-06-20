using UnityEngine;
using UnityEngine.Pool;


public class TankSingleShotWeapon : TankWeaponController
{
    private void Awake()
    {
        shellPool = new ObjectPool<TankShell>(
            () => Instantiate(tankWeapon.shellPrefab).Setup(this),
            shell =>
            {
                shell.Respawn();
                Reset();
            },
            shell => shell.Despawn(),
            shell => Destroy(shell.gameObject),
            false,
            initPoolSize,
            20);
        
        Reset();
    }
    
    public override void Fire()
    {
        if (reloadTimer > 0)
            return;

        shellPool.Get();
    }

    public override void Reload()
    {
        if (reloadTimer > 0)
            reloadTimer -= Time.deltaTime;
    }

    public override void Reset()
    {
        reloadTimer = tankWeapon.reloadTime;
    }
}