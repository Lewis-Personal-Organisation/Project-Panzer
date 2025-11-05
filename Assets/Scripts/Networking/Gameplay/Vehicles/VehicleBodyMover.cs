using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputStates = VehicleInputManager.InputState;

public class VehicleBodyMover : LocalVehicleComponent
{
    private float inputSpeed;
    private float targetInputSpeed => vehicle.inputManager.moveInput switch
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
        OnGUISceneViewData.AddOrUpdateLabel("Input State", $"{vehicle.inputManager.vehicleState}", Color.black);

        if (partiallyGrounded)
        {
            switch (vehicle.inputManager.vehicleState)
            {
                case InputStates.MovingBackward or InputStates.MovingBackwardAndRotating:
                    
                    // Reset input speed, so we don't move forwards immediately (velocity would be positive when steer velocity is applied)
                    if (vehicle.inputManager.lastInputState == InputStates.Rotating && vehicle.mobility.steerVelocity > 0)
                        inputSpeed = 0;

                    inputSpeed = Mathf.MoveTowards(inputSpeed, targetInputSpeed, vehicle.mobility.speedDelta * Time.deltaTime);
                    break;

                case InputStates.MovingForward or InputStates.MovingForwardAndRotating:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, targetInputSpeed, vehicle.mobility.speedDelta * Time.deltaTime);
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

        OnGUISceneViewData.AddOrUpdateLabel("Input Speed: ", $"{inputSpeed}", Color.black);

        // If Rotating, target speed more than allowed and velocity is higher than allowed
        if (vehicle.inputManager.vehicleState != InputStates.Rotating || vehicle.velocityTracker.z.velocity <= vehicle.mobility.steerVelocity)
        {
            vehicle.hullRigidbody.AddRelativeForce(Vector3.forward * inputSpeed * vehicle.mobility.forceMultiplier * Time.deltaTime, ForceMode.Force);
        }

        // Add Gravity
        vehicle.hullRigidbody.AddForce(vehicle.gravitationalForce * vehicle.hullRigidbody.mass * Vector3.down);

        // Set Velocity
        vehicle.hullRigidbody.velocity = transform.TransformDirection(new Vector3(0, vehicle.velocityTracker.y.velocity, vehicle.velocityTracker.z.Clamped(-vehicle.mobility.backwardSpeed, vehicle.mobility.forwardSpeed)));
        OnGUISceneViewData.AddOrUpdateLabel("Velocity: ", $"{vehicle.velocityTracker.z.Clamped(-vehicle.mobility.backwardSpeed, vehicle.mobility.forwardSpeed)}", Color.black);

        vehicle.inputManager.SetLastInputState();
    }
}