using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleGroundDetector : LocalVehicleComponent
{
    [SerializeField] private Transform leftCastPoint;
    [SerializeField] private Transform rightCastPoint;
    [SerializeField] private float castDistance = 0.03F;
    public bool leftSideIsGrounded { get; private set; } = false;
    public bool rightSideIsGrounded { get; private set; } = false;
    public bool FullyGrounded => leftSideIsGrounded && rightSideIsGrounded;
    public bool PartiallyGrounded => leftSideIsGrounded || rightSideIsGrounded;
    private Vector3 lastFramePos;
    [SerializeField] private PhysicMaterial physicsMaterial;
    
    
    public void DetectGroundState()
    {
        // Check ground casts
        bool leftGroundedThisFrame = Physics.Raycast(leftCastPoint.position, leftCastPoint.forward, castDistance, LayerMask.GetMask("Ground", "PlayerAimDetectable"));
        bool rightGroundedThisFrame = Physics.Raycast(rightCastPoint.position, rightCastPoint.forward, castDistance, LayerMask.GetMask("Ground", "PlayerAimDetectable"));
        
        // If state has not changed, return
        if (leftGroundedThisFrame == leftSideIsGrounded && rightGroundedThisFrame == rightSideIsGrounded) return;

        leftSideIsGrounded = leftGroundedThisFrame;
        rightSideIsGrounded = rightGroundedThisFrame;

        if (leftSideIsGrounded && rightSideIsGrounded)
        {
            vehicle.gravitationalForce = vehicle.mobility.globalGravity;
            physicsMaterial.bounciness = vehicle.mobility.physicsBounciness;
        }
        else if (!leftSideIsGrounded && !rightSideIsGrounded)
        {
            if ((vehicle.transform.position - lastFramePos).sqrMagnitude >= 0.5F)
            {
                vehicle.gravitationalForce = vehicle.mobility.localGravity;
            }
            // Stuck detection - Unused for now
            // else
            // {
            //     bool tiltIsForwards = Vector3.SignedAngle(transform.up, Vector3.up, transform.right) > 0;
            //     Debug.Log($"Stuck!! {Vector3.SignedAngle(transform.forward, Vector3.forward, Vector3.up)}");
            // }

            physicsMaterial.bounciness = 0;
            lastFramePos = vehicle.hullBoneTransform.position;
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(leftCastPoint.position, leftCastPoint.forward * castDistance);
        Gizmos.DrawRay(rightCastPoint.position, rightCastPoint.forward * castDistance);
    }
}
