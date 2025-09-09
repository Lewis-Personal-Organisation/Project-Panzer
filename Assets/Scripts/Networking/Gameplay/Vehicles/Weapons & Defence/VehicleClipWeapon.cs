using UnityEngine;
using UnityEngine.Pool;

public class VehicleClipWeapon : VehicleWeaponController
{
    [SerializeField] private int clipSize;
    [SerializeField] private int shellsInClip;
    [SerializeField] private float shotDelayTime;
    [SerializeField] private float shotDelayTimer;

    private void Start()
    {
        shellPool = new ObjectPool<TankShell>(
            () => Instantiate(vehicleWeapon.shellPrefab).Setup(this),
            shell =>
            {
                shell.Respawn();
                ResetWeapon();
                shellsInClip--;
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
        if (shellsInClip == 0 )
            return;
        
        if (shotDelayTimer > 0)
            return;

        // Fire
        shellPool.Get();
        PrepareLean();
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
                reloadTimer = vehicleWeapon.reloadTime;
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
        reloadTimer = vehicleWeapon.reloadTime;        // Reset this for balance purposes
        shotDelayTimer = shotDelayTime;
    }
}
