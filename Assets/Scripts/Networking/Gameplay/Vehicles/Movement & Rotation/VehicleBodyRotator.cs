using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleBodyRotator : LocalVehicleComponent
{
    private new void Awake()
    {
        base.Awake();
    }

    public void RotateTank()
    {
        vehicle.hullRigidbody.angularDrag = vehicle.inputManager.rotationInput == 0 ? vehicle.mobility.straightSteerDrag : vehicle.mobility.steeringDrag;
        vehicle.hullRigidbody.maxAngularVelocity = vehicle.mobility.maxAngularVelocity;
        // Torque
        vehicle.hullRigidbody.AddTorque(Vector3.up * vehicle.inputManager.rotationInput * vehicle.mobility.turnSpeed * Time.deltaTime * vehicle.mobility.torqueMultipler, ForceMode.Force);
    }

    // Tilt Correction. Not used currently as Gravity does the same job for us, for now!
    // else
    // {
    // else, apply an auto rotation
    // If rotation is higher than value, force rotation back
    // float zAngle = this.transform.rotation.eulerAngles.z;
    // if (Mathf.Abs(zAngle) > mobility.minAngleForCorrection)
    // {
    // hullRigidbody.AddTorque(hullBoneTransform.forward * mobility.correctionTorqueForce * (zAngle > 0 ? -1 : 1), ForceMode.Force);

    // Left or Right bound edge based on angle
    // hullRigidbody.maxAngularVelocity = mobility.maxAngularVelocity;
    // hullRigidbody.PivotAroundPoint(hullCollider.PointAlongBounds(zAngle > 0 ? -1 : 1, -1), hullBoneTransform.forward, mobility.correctionTorqueForce);
    // }
    // }
}
