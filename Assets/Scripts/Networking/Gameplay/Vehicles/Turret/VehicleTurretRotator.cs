using System;
using UnityEngine;

public class VehicleTurretRotator : VehicleComponent
{
    [SerializeField] private new CameraController cameraController;
    [SerializeField] private Transform turretTransform;


    public void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;
        cameraController = vehicle.cameraController;
        cameraController.OnProcessCamera += RotateTankTurret;
    }

    private void RotateTankTurret()
    {
        Quaternion target = Quaternion.Euler(0F, cameraController.transform.eulerAngles.y, 0F);
        turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, target, Time.deltaTime * vehicle.mobility.turretRotationSpeed);
    }
}

#region Old Rotation Method
// [SerializeField] private Transform targetTransform;
// private Vector3 mousePoint => cameraController.mouseCastState.point;
// private void RotateTankTurret()
// {
//     if (mousePoint == Vector3.zero)
//         return;
//         
//     // move the target object to the hit position
//     targetTransform.position = mousePoint;
//         
//     // Rotate turret
//     Vector3 lookRot = vehicle.hullBoneTransform.worldToLocalMatrix.MultiplyPoint(mousePoint).ReplaceY(0);
//     OnGUISceneViewData.AddOrUpdateLabel("Look Point: ", $"{lookRot,8:#.0000000}");
//         
//     if (lookRot == Vector3.zero)
//         return;
//         
//     Quaternion targetRotation = Quaternion.LookRotation(lookRot);
//     turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, targetRotation, Time.deltaTime * vehicle.mobility.turretRotationSpeed);
//         
//     Debug.DrawLine(turretTransform.position, turretTransform.position + turretTransform.forward * 15F, Color.red);
// }
#endregion
