using System;
using UnityEngine;
using UnityEngine.Events;

public class VehicleTurretRotator : VehicleComponent, IVehicleComponentToggleable
{
    [SerializeField] private new CameraController cameraController;
    [SerializeField] private Transform turretTransform;
    private UnityAction OnProcessTurret;

    public void Setup(VehicleController vehicleController)
    {
        vehicle = vehicleController;
        cameraController = vehicle.cameraController;

        Enable();
    }

    private void LateUpdate()
    {
        cameraController?.OnProcessCamera?.Invoke();
        OnProcessTurret?.Invoke();
    }

    private void ProcessTurret()
    {
        Quaternion target = Quaternion.Euler(0F, cameraController.transform.eulerAngles.y, 0F);
        turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, target, Time.deltaTime * vehicle.mobility.turretRotationSpeed);
    }

    public void Enable()
    {
        OnProcessTurret += ProcessTurret;
    }
    
    public void Disable()
    {
        OnProcessTurret = null;
    }
}