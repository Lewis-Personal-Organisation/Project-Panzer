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

    protected override void ResetWeapon()
    {
        reloadTimer = tankWeapon.reloadTime;        // Reset this for balance purposes
        shotDelayTimer = shotDelayTime;
    }

    // protected override void PrepareLean()
    // {
    //     Vector3 shellForwardInHullSpace = tankController.hullBoneTransform.InverseTransformDirection(shellSpawnPoint.forward);
    //     turretCross = Vector3.Cross(shellForwardInHullSpace, Vector3.up);
    //     xLean = 0;
    //     zLean = 0;
    //     xStep = tankWeapon.xLeanMax * Mathf.Abs(turretCross.x) * (1F / tankWeapon.leanTime);
    //     zStep = tankWeapon.zLeanMax * Mathf.Abs(turretCross.z) * (1F / tankWeapon.leanTime);
    //     currentLeanTime = 0;
    //     reverse = false;
    // }
    //
    // /// <summary>
    // /// Applies lean for firing the weapon
    // /// </summary>
    // protected override void ApplyLean()
    // {
    //     currentLeanTime += Time.deltaTime * (reverse ? -1F : 1F);
    //
    //     if (!reverse && currentLeanTime >= tankWeapon.leanTime * 0.5F)
    //         reverse = true;
    //
    //     xLean = Mathf.MoveTowards(xLean, reverse ? 0 : tankWeapon.xLeanMax * turretCross.x, Time.deltaTime * xStep);
    //     zLean = Mathf.MoveTowards(zLean, reverse ? 0 : tankWeapon.zLeanMax * turretCross.z, Time.deltaTime * zStep);
    //     tankController.leanManager.UpdateWeaponLean(xLean, zLean);
    // }
}
