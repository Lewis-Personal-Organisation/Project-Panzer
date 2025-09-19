using UnityEngine;

public class VehicleWeaponLeanController : VehicleLeanController
{
    private VehicleWeaponController weaponController;

    public override float LeanX => baseXLean * easedXLean;
    public override float LeanZ => baseZLean * easedZLean;

    protected float easedXLean;
    protected float easedZLean;

    // Lean Values
    private float xStep;
    private float zStep;
    private float currentLeanTime = 0;
    private bool reverse = false;
    private Vector3 turretCross;

    [SerializeField] private Ease leanInFunction;
    [SerializeField] private Ease leanOutFunction;

    public bool shouldLean { get; private set; }


    public void Setup(VehicleWeaponController newVehicleWeaponController)
    {
        this.weaponController = newVehicleWeaponController;
    }
    
    public void PrepareLean()
    {
        Vector3 shellForwardInHullSpace = vehicle.hullBoneTransform.InverseTransformDirection(weaponController.shellSpawnPoint.forward);
        turretCross = Vector3.Cross(shellForwardInHullSpace, Vector3.up);
        xStep = weaponController.weapon.xLeanMax * Mathf.Abs(turretCross.x) * (1F / weaponController.weapon.leanTime);
        zStep = weaponController.weapon.zLeanMax * Mathf.Abs(turretCross.z) * (1F / weaponController.weapon.leanTime);
        shouldLean = true;
        currentLeanTime = 0;
        reverse = false;
    }
    
    public override void UpdateLeanValues()
    {
        if (!enabled)
            return;
        
        currentLeanTime += Time.deltaTime * (reverse ? -1F : 1F);
        float normalizedLean = (currentLeanTime / weaponController.weapon.leanTime) * 2F;
        
        // Reverse the UpdateLeanValues at midpoint
        if (!reverse && currentLeanTime >= weaponController.weapon.leanTime * 0.5F)
            reverse = true;

        baseXLean = Mathf.MoveTowards(baseXLean, reverse ? 0 : weaponController.weapon.xLeanMax * turretCross.x, Time.deltaTime * xStep);
        baseZLean = Mathf.MoveTowards(baseZLean, reverse ? 0 : weaponController.weapon.zLeanMax * turretCross.z, Time.deltaTime * zStep);

        // Apply Ease function
        easedXLean = Easer.Calculate(reverse ? leanInFunction : leanOutFunction, normalizedLean);
        easedZLean = Easer.Calculate(reverse ? leanInFunction : leanOutFunction, normalizedLean);
        
        if (currentLeanTime <= 0)
            shouldLean = false;
    }
}
