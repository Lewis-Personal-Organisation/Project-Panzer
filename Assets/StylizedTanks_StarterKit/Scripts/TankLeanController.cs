using UnityEngine;

public class TankLeanController : MonoBehaviour
{
    private float xLean = 0.0f;
    private float x = 0;
    [SerializeField] private float zCurrentLean = 0.0f;
    [SerializeField] private float zTargetLean = 0.0f;
    [SerializeField] private float restingHullVertLean;

    public Transform hullBoneTransform;
    public TankMobility data; 
    private Vector3 localVelocity;
    private float velocityMultiplier => localVelocity.z > 0F ? -1F : localVelocity.z < 0F ? 1F : 0F;
    private float turnInputValue;
    private float moveInput = 0F;


    private void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");

        localVelocity.z = moveInput * 10;
        
        ApplyTankLean();
    }

    
    private void ApplyTankLean()
    {
        // Hull lean Z
        zTargetLean = x < 0.5F ? velocityMultiplier * data.verticalMaxLean : velocityMultiplier * restingHullVertLean;
        zCurrentLean = Mathf.Lerp(zCurrentLean, zTargetLean, Time.deltaTime * data.verticalLeanSpeed);

        if (Mathf.Abs(zCurrentLean) > Mathf.Abs(zTargetLean) - 1.0f && Mathf.Abs(zCurrentLean) > Mathf.Abs(0.01F))
        {
            x = Mathf.Clamp(x + Time.deltaTime, 0, 0.5F);
        }
        else
        {
            x = 0;
        }

        // Hull lean X
        xLean = Mathf.Lerp(xLean, turnInputValue * data.horizontalMaxLean, Time.deltaTime * data.horizontalLeanSpeed);
        hullBoneTransform.localRotation = Quaternion.Euler(zCurrentLean, 0, xLean);
    }
}