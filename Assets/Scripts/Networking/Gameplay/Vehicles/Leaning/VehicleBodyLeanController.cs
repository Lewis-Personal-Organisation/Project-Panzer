using System;
using UnityEditor;
using UnityEngine;

public class VehicleBodyLeanController : VehicleLeanController
{
    public override float LeanX => baseXLean;
    public override float LeanZ => baseZLean;

    private float xLeanTimer;
    private float xTargetLean;
    private float XLeanTimerMax = 0.5F;

    private VehicleMobility data => vehicle.mobility;

    [SerializeField] private LocalZVelocityTracker velocityTracker;

    // Returns tilt value based on move input and velocity. Order of these items is important!
    private float tilt => (vehicle.inputManager.intMoveInput, velocityTracker.velocity) switch
    {
        // Steering only tilt
        (0, > 0) when vehicle.inputManager.turnInputValue != 0 => -data.verticalMaxLean,         // No input, some velocity, and turning
        
        // Brake tilt
        (0 or -1, var vel) when vel > data.cruiseForwardVelocity => data.verticalMaxLean,                       // No/Neg input in cruise - braking effect
        (0 or -1, var vel) when vel > data.minForwardVelocity => data.verticalMaxLean,                          // No/Neg input more than min - braking effect
        
        // Forward Accel tilt
        (1, var vel) when vel > data.cruiseForwardVelocity => data.verticalRestingLean,                         // Accel and in cruise - Cruise lean
        (1, > 0 or < 0) => -data.verticalMaxLean,                                                     // Accel and more than min - Max lean
        
        // backward brake tilt
        (-1, var vel) when vel < -data.minForwardVelocity => data.verticalMaxLean,                              // Deccel and less than min - Negative Max lean
        
        // Rolling only tilt
        (0, var vel) when vel > data.minForwardVelocity => data.verticalMaxLean,                                // No Input and more than min - Negative Max lean
        (0, var vel) when vel < -data.minForwardVelocity => -data.verticalMaxLean,                              // No Input and less than min - Max Lean
        _ => 0                                                                                                  // No tilt
    };
    
    
    public override void UpdateLeanValues()
    {
        if (!enabled) return;
        
        // Hull lean X
        baseXLean = Mathf.Lerp(baseXLean, tilt, data.verticalLeanSpeed * Time.deltaTime);

        // Hull lean Z
        baseZLean = Mathf.Lerp(baseZLean, vehicle.inputManager.turnInputValue * data.horizontalMaxLean, Time.deltaTime * data.horizontalLeanSpeed);
    }
}