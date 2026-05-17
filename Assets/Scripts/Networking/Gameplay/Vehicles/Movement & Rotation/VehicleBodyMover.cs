using UnityEngine;
using InputStates = VehicleInputManager.InputState;

public class VehicleBodyMover : LocalVehicleComponent
{
    private float inputSpeed;
    
    public float RetainedVelocity
    {
        get => velocityMultiplier;
        set
        {
            value = Mathf.Clamp(value, 0, 1);
            velocityMultiplier = Mathf.Lerp(1, 0.80F, value);       // 0.9714% at 0.143
        }
    }
    private float velocityMultiplier = 0;
    
    private float TargetInputSpeed => vehicle.inputManager.moveInput switch
    {
        1 => 1,
        -1 => -1,
        _ => 0
    };
    
    
    /// <summary>
    /// Moves the tank based on its grounded state, input values and gravity
    /// </summary>
    public void MoveTank(bool partiallyGrounded)
    {
        // SceneData.Label("Input State", $"{vehicle.inputManager.vehicleState}", 10, 30, 550, 25, Color.black);

        if (partiallyGrounded)
        {
            switch (vehicle.inputManager.vehicleState)
            {
                case InputStates.MovingBackward or InputStates.MovingBackwardAndRotating:
                    
                    // Reset input speed, so we don't move forwards immediately (velocity would be positive when steer velocity is applied)
                    if (vehicle.inputManager.lastInputState == InputStates.Rotating && vehicle.mobility.steerVelocity > 0)
                        inputSpeed = 0;

                    inputSpeed = Mathf.MoveTowards(inputSpeed, TargetInputSpeed, vehicle.mobility.speedDelta * Time.deltaTime);
                    break;

                case InputStates.MovingForward or InputStates.MovingForwardAndRotating:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, TargetInputSpeed, vehicle.mobility.speedDelta * Time.deltaTime);
                    break;

                case InputStates.Rotating:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, 1, vehicle.mobility.speedDelta * Time.deltaTime);
                    break;

                case InputStates.None:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, 0, vehicle.mobility.brakeDelta * Time.deltaTime);
                    break;
            }
        }
        else
        {
            inputSpeed = Mathf.MoveTowards(inputSpeed, 0, vehicle.mobility.speedDelta * Time.deltaTime);
        }

        // SceneData.Label("Input Speed: ", $"{inputSpeed}", 10, 40, 550, 25, Color.black);

        // If Rotating, target speed more than allowed and velocity is higher than allowed
        if (vehicle.inputManager.vehicleState != InputStates.Rotating || vehicle.velocityTracker.z.velocity <= vehicle.mobility.steerVelocity)
        {
            vehicle.hullRigidbody.AddRelativeForce(Vector3.forward * inputSpeed * vehicle.mobility.forceMultiplier, ForceMode.Force);
        }

        // Add Gravity
        vehicle.hullRigidbody.AddForce(vehicle.gravitationalForce * vehicle.hullRigidbody.mass * Vector3.down);

        // Get Local velocity as vector
        Vector3 localVelocity = transform.InverseTransformDirection(vehicle.hullRigidbody.velocity);
        
        // Clamp Z velocity
        localVelocity.z = Mathf.Clamp(
            localVelocity.z,
            -vehicle.mobility.backwardSpeed,
            vehicle.mobility.forwardSpeed
        );
        SceneData.Label("Velocity: ", $"{vehicle.velocityTracker.z.Clamped(-vehicle.mobility.backwardSpeed, vehicle.mobility.forwardSpeed)}", 10, 60, 550, 25, Color.black);
        
        // Set the local velocity multiplier
        localVelocity.x *= RetainedVelocity;
        
        // Write back the new velcoity
        vehicle.hullRigidbody.velocity = transform.TransformDirection(localVelocity);
        

        vehicle.inputManager.SetLastInputState();
    }
}