using UnityEngine;

public class TankLeanController : VehicleComponent
{
    private float xLean = 0.0f;
    private float xTargetLean = 0;
    private float xLeanTimer = 0F;
    private const float XLeanTimerMax = 0.5F;
    private float zLean = 0.0f;

    public Transform hullBoneTransform;
    public VehicleMobility data; 
    private Vector3 localVelocity;
    private float velocityMultiplier => localVelocity.z > 0F ? -1F : localVelocity.z < 0F ? 1F : 0F;
    internal float tiltSign => velocityMultiplier > data.minForwardVelocity ? -1F : velocityMultiplier < -data.minForwardVelocity ? 1F : 0F;


    public void Setup(VehicleController controller)
    {
        vehicle = controller;
    }
    
    private void FixedUpdate()
    {
        localVelocity.z = vehicle.inputManager.moveInput * 10;
        
        ApplyTankLean();
    }

    
    private void ApplyTankLean()
    {
        // Hull UpdateLeanValues X
        float leanMode = xLeanTimer < XLeanTimerMax ? data.verticalMaxLean : data.verticalRestingLean; // Vertical or resting lean value
        xTargetLean = leanMode * tiltSign;                                                             // Should the value be pos or neg? (Moving forwards should lean backward etc.)
        xLean = Mathf.Lerp(xLean, xTargetLean, Time.deltaTime * data.verticalLeanSpeed);               // Move value used for the lean rotation

        // If current lean is above target (minus a subtracted value), increment timer. Else, reset timer
        if (Mathf.Abs(xLean) > Mathf.Abs(xTargetLean) - data.verticalToRestingLeanValue)
        {
            xLeanTimer = Mathf.Clamp(xLeanTimer + Time.deltaTime, 0, XLeanTimerMax);
        }
        else
        {
            xLeanTimer = 0;
        }

        // Hull lean Z
        zLean = Mathf.Lerp(zLean, vehicle.inputManager.turnInputValue * data.horizontalMaxLean, Time.deltaTime * data.horizontalLeanSpeed);
    }
}