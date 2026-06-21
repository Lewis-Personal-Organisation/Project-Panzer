

using System;
using UnityEngine;
using UnityEngine.Events;
public class VehicleStuckManager : NetworkedVehicleComponent
{
    public float minPos = 0;
    private Vector3 lastPos;
    private Vector3 deltaPos;
    public bool isStuck = false;
    public bool isMoving = false;
    public bool tryingToMove = false;

    public float stuckTimer = 0;
    public float stuckTimerMax = 4F;
    public float stuckTime;
    public float stuckHelperTime;

    private Vector3 safePosition;
    private Quaternion safeRotation;
    private float recoveryTimer = 0;
    public Transform safePosMarker;
    
    
    public void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;

        safePosition = vehicle.transform.position;
        safeRotation = vehicle.transform.rotation;
    }
    
    private void FixedUpdate()
    {
        if (vehicle.gameObject.transform == null)
            return;

        // Debug sweep detection
        // vehicle.hullRigidbody.SweepTest(vehicle.transform.forward, out RaycastHit rHit, 7F);
        // Debug.DrawLine(vehicle.transform.position, rHit.point == Vector3.zero ? vehicle.transform.forward * 7F : rHit.point, Color.red);
        
        // Safe Position Detection
        // Cache a safe position only when we are moving, arent stuck and a sweeptest is made
        if (recoveryTimer > stuckTimerMax)
        {
            if (isMoving && isStuck == false && !vehicle.hullRigidbody.SweepTest(vehicle.transform.forward, out var hit, 7F))
            {
                safePosition = vehicle.transform.position;
                safeRotation = vehicle.transform.rotation;
                
                if (safePosMarker != null)
                    safePosMarker.position = safePosition;
                
                recoveryTimer = 0;
            }
        }
        else
        {
            if (isMoving && isStuck == false)
            {
                recoveryTimer += Time.deltaTime;
            }
        }
        
        // Stuck Detection
        // Find the absolute distances
        deltaPos = new Vector3(Mathf.Abs(vehicle.gameObject.transform.position.x - lastPos.x), Mathf.Abs(vehicle.gameObject.transform.position.y - lastPos.y), Mathf.Abs(vehicle.gameObject.transform.position.z - lastPos.z));

        // Cache last pos
        lastPos =  vehicle.gameObject.transform.position;
        
        isMoving = deltaPos.x >= minPos || deltaPos.y >= minPos || deltaPos.z >= minPos;
        tryingToMove = vehicle.inputManager.vehicleState != VehicleInputManager.InputState.None;
        isStuck = !isMoving && tryingToMove;

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
        
        // Cancel out abilitiy to reposition x time after becoming stuck
        if (GameplayUI.PauseMenu.showRepositionOption && Time.time > stuckTime + stuckHelperTime)
        {
            GameplayUI.PauseMenu.showRepositionOption = false;
            Debug.Log($"{ Time.time} > {stuckTime} + {stuckHelperTime}");
        }
    }

    public void Unstick()
    {
        GameplayUI.RepositionUI.FadeForPlayerReposition(safePosition, safeRotation);
    }
}
