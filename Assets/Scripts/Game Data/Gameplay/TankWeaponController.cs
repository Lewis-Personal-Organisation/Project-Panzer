using System;
using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(TankController))]
public abstract class TankWeaponController : MonoBehaviour
{
    protected TankWeapon tankWeapon => tankController.data.weapon;
    [SerializeField] protected int initPoolSize;
    [field: HideInInspector] public TankController tankController { get; protected set; }
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
    public abstract void ResetWeapon();

    /// <summary>
    /// Called when this component is added to a gameobject.
    /// With RequireComponent attribute above, ensures we always have reference to the correct Tank Controller
    /// </summary>
    private void Reset()
    {
        if (tankController == null)
            tankController = GetComponent<TankController>();
    }
}
