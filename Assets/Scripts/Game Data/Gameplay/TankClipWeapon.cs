using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;

public class TankClipWeapon : TankWeaponController
{
    [SerializeField] private int clipSize;
    [SerializeField] private int shellsInClip;
    [SerializeField] private float shotDelayTime;
    [SerializeField] private float shotDelayTimer;

    private void Start()
    {
        shellPool = new ObjectPool<TankShell>(
            () => Instantiate(tankWeapon.shellPrefab).Setup(this),
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
            20);
        
        ResetWeapon();
    }
    
    public override void Fire()
    {
        if (shellsInClip == 0 )
            return;
        
        if (shotDelayTimer > 0)
            return;

        // Fire
        shellPool.Get();
    }

    public override void Reload()
    {
        // If not at max shells
        if (shellsInClip != clipSize)
        {
            // If timer done, refill a shell and reset timer. Else, count down
            if (reloadTimer <= 0)
            {
                shellsInClip++;
                reloadTimer = tankWeapon.reloadTime;
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

    public override void ResetWeapon()
    {
        reloadTimer = tankWeapon.reloadTime;        // Reset this for balance purposes
        shotDelayTimer = shotDelayTime;
    }
}
