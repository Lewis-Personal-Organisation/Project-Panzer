using UnityEngine;

public class TankLeanController : MonoBehaviour
{
    private readonly TankController controller;
    private float xWeaponLean;
    private float zWeaponLean;
    private float xLean = 0.0f;
    private float xTargetLean = 0;
    private float xLeanTimer = 0F;
    private const float XLeanTimerMax = 0.5F;
    private float zLean = 0.0f;
    [SerializeField] private float zCurrentLean = 0.0f;
    [SerializeField] private float zTargetLean = 0.0f;
    [SerializeField] private float restingHullVertLean;

    public Transform hullBoneTransform;
    public TankMobility data; 
    private Vector3 localVelocity;
    private float velocityMultiplier => localVelocity.z > 0F ? -1F : localVelocity.z < 0F ? 1F : 0F;
    private float turnInputValue;
    private float moveInput = 0F;
    internal float tiltSign => velocityMultiplier > data.minForwardVelocity ? -1F : velocityMultiplier < -data.minForwardVelocity ? 1F : 0F;

    private void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");

        localVelocity.z = moveInput * 10;
        
        ApplyTankLean();
    }

    
    private void ApplyTankLean()
    {
        // Hull Lean X
        float leanMode = xLeanTimer < XLeanTimerMax ? data.verticalMaxLean : data.verticalRestingLean;  // Vertical or resting lean value
        xTargetLean = leanMode * tiltSign;                                                              // Should the value be pos or neg? (Moving forwards should lean backward etc.)
        xLean = Mathf.Lerp(xLean, xTargetLean, Time.deltaTime * data.verticalLeanSpeed);                // Move value used for the lean rotation
            
        //If current lean is above target (minus a subtracted value), increment timer. Else, reset timer
        if (Mathf.Abs(xLean) > Mathf.Abs(xTargetLean) - data.verticalToRestingLeanValue)
        {
            xLeanTimer = Mathf.Clamp(xLeanTimer + Time.deltaTime, 0, XLeanTimerMax);
        }
        else
        {
            xLeanTimer = 0;
        }

        // Hull lean Z
        zLean = Mathf.Lerp(zLean, turnInputValue * data.horizontalMaxLean, Time.deltaTime * data.horizontalLeanSpeed);
            
        // Apply Lean
        hullBoneTransform.localRotation = Quaternion.Euler(xLean + xWeaponLean, 0, zLean + zWeaponLean);
    }
}