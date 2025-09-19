using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(VehicleController))]
public abstract class VehicleWeaponController : VehicleComponent
{
    [field: SerializeField] public VehicleWeapon weapon { get; protected set; }
    [field: SerializeField] public ObjectPool<WeaponShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected VehicleWeaponLeanController weaponLeanController;
    [SerializeField] protected int initPoolSize;
    protected float reloadTimer = 0;
    
    
    private new void Awake()
    {
        base.Awake();
        TryGetLocalComponent(ref weaponLeanController);
        weaponLeanController.Setup(this);
    }

    private void Update()
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
    }
    protected abstract void Fire();
    protected abstract void Reload();
    protected abstract void ResetWeapon();
}
