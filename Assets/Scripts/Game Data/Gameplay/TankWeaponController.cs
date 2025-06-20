using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;

public abstract class TankWeaponController : MonoBehaviour
{
    protected TankWeapon tankWeapon => tankController.data.tankWeapon;
    [SerializeField] protected int initPoolSize;
    [field: SerializeField] public TankController tankController { get; private set; }
    [field: SerializeField] public ObjectPool<TankShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected float reloadTimer = 0;
    
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
            return;
        }
        
        Reload();
    }
    public abstract void Fire();
    public abstract void Reload();
    public new abstract void Reset();
}
