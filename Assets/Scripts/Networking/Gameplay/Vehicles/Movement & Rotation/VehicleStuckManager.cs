

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
    
    private UnityAction OnStuck = null;

    private Vector3 safePosition;
    private float safePosTimer = 0;
    public Transform safePosMarker;
    
    
    public void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;

        safePosition = vehicle.transform.position;
        
        OnStuck += () =>
        {
            // Reposition player - Fire force backwards from the facing direction
            Debug.Log("OnStuck!!");
            vehicle.hullRigidbody.MovePosition(safePosition);
            // vehicle.hullRigidbody.AddRelativeForce(Vector3.back * repelForce, ForceMode.Impulse);
        };
    }
    
    private void FixedUpdate()
    {
        if (vehicle.gameObject.transform == null)
            return;

        vehicle.hullRigidbody.SweepTest(vehicle.transform.forward, out RaycastHit rHit, 7F);
        
        Debug.DrawLine(vehicle.transform.position, rHit.point == Vector3.zero ? vehicle.transform.forward * 7F : rHit.point, Color.red);
        
        // Cache a safe position only when we are moving, arent stuck and a sweeptest is made
        if (safePosTimer > 1.5F)
        {
            if (isMoving && isStuck == false && !vehicle.hullRigidbody.SweepTest(vehicle.transform.forward, out var hit, 7F))
            {
                safePosition = vehicle.transform.position;
                safePosMarker.position = safePosition;
                safePosTimer = 0;
            }
        }
        else
        {
            if (isMoving && isStuck == false)
            {
                safePosTimer += Time.deltaTime;
            }
        }
        
        // Find the absolute distances
        deltaPos = new Vector3(Mathf.Abs(vehicle.gameObject.transform.position.x - lastPos.x), Mathf.Abs(vehicle.gameObject.transform.position.y - lastPos.y), Mathf.Abs(vehicle.gameObject.transform.position.z - lastPos.z));

        isMoving = deltaPos.x >= minPos || deltaPos.y >= minPos || deltaPos.z >= minPos;
        tryingToMove = vehicle.inputManager.vehicleState != VehicleInputManager.InputState.None;

        isStuck = isMoving == false && tryingToMove;

        if (isStuck)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimerMax)
            {
                OnStuck?.Invoke();
                // OnStuck = null;
            }
        }
        else
        {
            stuckTimer = 0;
        }
        
        
        // Cache last pos
        lastPos =  vehicle.gameObject.transform.position;
    }
}
