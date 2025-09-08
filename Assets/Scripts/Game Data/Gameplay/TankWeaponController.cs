using System;
using MiniTanks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

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

    [Header("Lean")]
    public float xLean;         // The lean on the X and Z axis
    internal float xStep;
    public float zLean;
    internal float zStep;
    public float currentLeanTime;
    internal bool reverse = false;
    public Vector3 turretCross;
    
    
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
        Vector3 shellForwardInHullSpace = tankController.hullBoneTransform.InverseTransformDirection(shellSpawnPoint.forward);
        turretCross = Vector3.Cross(shellForwardInHullSpace, Vector3.up);
        xLean = 0;
        zLean = 0;
        xStep = tankWeapon.xLeanMax * Mathf.Abs(turretCross.x) * (1F / tankWeapon.leanTime);
        zStep = tankWeapon.zLeanMax * Mathf.Abs(turretCross.z) * (1F / tankWeapon.leanTime);
        currentLeanTime = 0;
        reverse = false;
    }

    /// <summary>
    /// Applies lean for firing the weapon
    /// </summary>
    protected void ApplyLean()
    {
        currentLeanTime += Time.deltaTime * (reverse ? -1F : 1F);

        if (!reverse && currentLeanTime >= tankWeapon.leanTime * 0.5F)
            reverse = true;

        xLean = Mathf.MoveTowards(xLean, reverse ? 0 : tankWeapon.xLeanMax * turretCross.x, Time.deltaTime * xStep);
        zLean = Mathf.MoveTowards(zLean, reverse ? 0 : tankWeapon.zLeanMax * turretCross.z, Time.deltaTime * zStep);
        tankController.leanManager.UpdateWeaponLean(xLean, zLean);
    }
    
    // protected abstract void PrepareLean();
    // protected abstract void ApplyLean();
}
