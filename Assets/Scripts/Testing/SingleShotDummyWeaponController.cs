using UnityEngine;
using UnityEngine.Pool;

public class SingleShotDummyWeaponController : DummyWeaponController
{
    private void Start()
    {
        shellPool = new ObjectPool<DummyWeaponShell>(
            () => Instantiate(weapon.shellPrefab).Setup(this),
            shell =>
            {
                Vector3 direction = targetVehicle ? targetVehicles[0].hullBoneTransform.position - this.transform.position : this.transform.position + this.transform.forward - this.transform.position;
                shell.Respawn(direction);
                ResetWeapon();
            },
            shell => shell.Despawn(),
            shell => Destroy(shell.gameObject),
            false,
            initPoolSize,
            weapon.ammoCount);

        ResetWeapon();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out VehicleController vehicle)) return;
        if (targetVehicles.Contains(vehicle)) return;
        
        targetVehicles.Add(vehicle);
        
        Debug.Log($"Added enemy {vehicle.gameObject.name} to detected list");
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out VehicleController vehicle)) return;
        if (!targetVehicles.Contains(vehicle))
            return;
        
        targetVehicles.Remove(vehicle);
        
        Debug.Log($"Removed enemy {vehicle.gameObject.name} from detected list");
    }

    protected override void Fire()
    {
        if (reloadTimer > 0)
            return;

        shellPool.Get();
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
}