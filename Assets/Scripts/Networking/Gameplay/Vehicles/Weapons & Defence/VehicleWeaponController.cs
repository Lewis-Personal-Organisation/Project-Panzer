using System;
using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(VehicleController))]
public abstract class VehicleWeaponController : MonoBehaviour
{
    [FormerlySerializedAs("tankWeapon")] [SerializeField] protected VehicleWeapon vehicleWeapon;
    [SerializeField] protected int initPoolSize;
    private VehicleController vehicleController;
    [field: SerializeField] public ObjectPool<TankShell> shellPool { get; protected set; }
    [field: SerializeField] public Transform shellSpawnPoint {get; private set;}
    protected float reloadTimer = 0;

    [Header("Lean")]
    private float xLean;         // The lean on the X and Z axis
    private float xStep;
    private float zLean;
    private float zStep;
    private float currentLeanTime;
    private bool reverse = false;
    private Vector3 turretCross;
    
    
    private void Awake()
    {
        if (vehicleController == null)
            vehicleController = GetComponent<VehicleController>();
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
        
        if (currentLeanTime >= 0)
            ApplyLean();
    }
    protected abstract void Fire();
    protected abstract void Reload();
    protected abstract void ResetWeapon();
    
    /// <summary>
    /// Prepares the Lean values for 
    /// </summary>
    protected void PrepareLean()
    {
        Vector3 shellForwardInHullSpace = vehicleController.hullBoneTransform.InverseTransformDirection(shellSpawnPoint.forward);
        turretCross = Vector3.Cross(shellForwardInHullSpace, Vector3.up);
        xLean = 0;
        zLean = 0;
        xStep = vehicleWeapon.xLeanMax * Mathf.Abs(turretCross.x) * (1F / vehicleWeapon.leanTime);
        zStep = vehicleWeapon.zLeanMax * Mathf.Abs(turretCross.z) * (1F / vehicleWeapon.leanTime);
        currentLeanTime = 0;
        reverse = false;
    }

    /// <summary>
    /// Applies lean for firing the weapon
    /// </summary>
    private void ApplyLean()
    {
        currentLeanTime += Time.deltaTime * (reverse ? -1F : 1F);

        if (!reverse && currentLeanTime >= vehicleWeapon.leanTime * 0.5F)
            reverse = true;

        xLean = Mathf.MoveTowards(xLean, reverse ? 0 : vehicleWeapon.xLeanMax * turretCross.x, Time.deltaTime * xStep);
        zLean = Mathf.MoveTowards(zLean, reverse ? 0 : vehicleWeapon.zLeanMax * turretCross.z, Time.deltaTime * zStep);
        vehicleController.LeanController.UpdateWeaponLean(xLean, zLean);
    }
}
