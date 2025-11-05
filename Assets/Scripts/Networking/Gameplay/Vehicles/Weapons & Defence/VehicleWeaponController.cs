using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(VehicleController))]
public abstract class VehicleWeaponController : NetworkedVehicleComponent
{
    [field: SerializeField] public VehicleWeapon weapon { get; protected set; }
    // [field: SerializeField] public ObjectPool<WeaponShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected VehicleWeaponLeanController weaponLeanController;
    [SerializeField] protected int initPoolSize;
    public float reloadTimer = 0;
    private UnityAction OnSimulate;


    public virtual void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;
        TryGetLocalComponent(ref weaponLeanController);
        weaponLeanController.Setup(this);
        
        // Add shooting loop
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

    private void Update()
    {
        OnSimulate?.Invoke();
    }
    protected abstract void Fire();
    protected abstract void Reload();
    protected abstract void ResetWeapon();

}
