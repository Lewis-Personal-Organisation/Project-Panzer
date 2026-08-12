

using System;
using UnityEngine;
using UnityEngine.Events;
public class VehicleStuckManager : NetworkedVehicleComponent
{
    public float minPos = 0;
    private Vector3 lastPos;
    public bool isStuck = false;
    public bool isMoving = false;

    public float stuckTimer = 0;
    public float stuckTimerMax = 4F;
    public float stuckTime;
    public float stuckHelperTime;

    private Vector3 safePosition;
    private Quaternion safeRotation;
    private float safePositionTimer = 0;
    public Transform safePosMarker;
    
    
    public void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;
        safePosition = vehicle.transform.position;
        safeRotation = vehicle.transform.rotation;
    }
    
    private void FixedUpdate()
    {
        
        // Don't manage non-owner objects or if the Player has external velocity applied
        if (!IsOwner || VehicleController.Instance.bodyMover.hasExternalVelocity)
            return;
        
        // Safe Position Caching - Cache a safe position only when we are moving, aren't stuck and a sweeptest is made
        if (safePositionTimer < stuckTimerMax)
        {
            if (isMoving && isStuck == false)
            {
                safePositionTimer += Time.deltaTime;

                if (!vehicle.hullRigidbody.SweepTest(vehicle.transform.forward, out var hit, 7F))
                {
                    safePosition = vehicle.transform.position;
                    safeRotation = vehicle.transform.rotation;
                
                    if (safePosMarker != null)
                        safePosMarker.position = safePosition;
                
                    safePositionTimer = 0;
                }
            }
        }
        
        // Stuck Detection
        // The absolute differences in for the last and current position
        Vector3 deltaPos = new Vector3(Mathf.Abs(vehicle.gameObject.transform.position.x - lastPos.x), Mathf.Abs(vehicle.gameObject.transform.position.y - lastPos.y), Mathf.Abs(vehicle.gameObject.transform.position.z - lastPos.z));

        // Cache last pos
        lastPos =  vehicle.gameObject.transform.position;
        
        // We're moving if the difference in pos meets thresholds, and stuck if we're not meeting thresholds but trying to move
        isMoving = deltaPos.x >= minPos || deltaPos.y >= minPos || deltaPos.z >= minPos;
        isStuck = !isMoving && vehicle.inputManager.vehicleState != VehicleInputManager.InputState.None;

        // If stuck, increase timer. Once met, allow reposition timer in menu
        if (isStuck)
        {
            stuckTimer = Mathf.Clamp(stuckTimer + Time.deltaTime, 0 , stuckTimerMax);
            stuckTime = Time.time;

            if (stuckTimer >= stuckTimerMax && !GameplayUI.PauseMenu.showRepositionOption)
            {
                GameplayUI.PauseMenu.showRepositionOption = true;
                return;
            }
        }
        else
        {
            stuckTimer = 0;
        }
        
        // Disbale Pause Menu reposition option after time limit
        if (GameplayUI.PauseMenu.showRepositionOption && Time.time > stuckTime + stuckHelperTime)
        {
            GameplayUI.PauseMenu.showRepositionOption = false;
        }
    }

    public void UnstickPlayer()
    {
        GameplayUI.RepositionUI.RepositionPlayer(safePosition, safeRotation);
    }
}
