using System;
using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(TankController))]
public abstract class TankWeaponController : MonoBehaviour
{
    [SerializeField] protected TankWeapon tankWeapon;
    [SerializeField] protected int initPoolSize;
    [field: HideInInspector] public TankController tankController { get; protected set; }
    [field: SerializeField] public ObjectPool<TankShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    protected float reloadTimer = 0;

    
    private void Awake()
    {
        if (tankController == null)
            tankController = GetComponent<TankController>();
    }

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
}
