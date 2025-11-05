using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class DummyWeaponController : MonoBehaviour
{
    public List<VehicleController> targetVehicles = new List<VehicleController>();
    [field: SerializeField] public DummyWeapon weapon { get; protected set; }
    [field: SerializeField] public ObjectPool<DummyWeaponShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    [SerializeField] protected int initPoolSize;
    protected float reloadTimer = 0;
    public bool targetVehicle = false;
    
    
    private void Update()
    {
        Reload();

        if (targetVehicle)
        {
            if (targetVehicles.Count > 0)
            {
                Fire();
            }
        }
        else
        {
            Fire();
        }
    }

    
    protected abstract void OnTriggerEnter(Collider other);
    protected abstract void OnTriggerExit(Collider other);
    protected abstract void Fire();
    protected abstract void Reload();
    protected abstract void ResetWeapon();
}
