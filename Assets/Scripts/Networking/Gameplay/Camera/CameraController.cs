using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MilkShake;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ProBuilder;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class CameraController : VehicleComponent, IVehicleComponentToggleable
{
    [SerializeField] private new Camera camera;
    public Shaker shaker;
    private ShakeInstance shakeInstance;
    [SerializeField] private IShakeParameters shakeData;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float yOffset;
    public UnityAction OnProcessCamera;

    
    [FormerlySerializedAs("camShiftRadius")]
    [SerializeField] private float lookAheadMax = 5F;
    [SerializeField] private float lookAheadDistance;
    [SerializeField] private bool hideCursor;
    private Vector3 orbitOffset;
    
    /// <summary>
    /// Sets up this camera for a players vehicle during gameplay
    /// </summary>
    /// <param name="vehicleController"></param>
    public void Setup(VehicleController vehicleController)
    {
        if (!camera && !this.transform.GetChild(0).TryGetComponent(out camera))
            Debug.LogError("CameraController :: Awake :: Camera Component not set or found!", this.gameObject);

        vehicle = vehicleController;
        targetTransform = vehicle.transform;
        
        this.transform.position = targetTransform.position + Vector3.up * yOffset + -targetTransform.forward * lookAheadMax;
        orbitOffset = transform.position - vehicle.hullBoneTransform.position;
        Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = hideCursor;
        
        // Enable Camera functionality
        Enable();
    }

    // private void LateUpdate()
    // {
    //     OnProcessCamera?.Invoke();
    // }
    
    /// <summary>
    /// The Method which process the cameras functionality
    /// </summary>
    private void ProcessCamera()
    {
        // Orbit this transform around the Y axis of the target
        orbitOffset = Quaternion.AngleAxis(vehicle.inputManager.MouseXDelta, Vector3.up) * orbitOffset;
        transform.position = vehicle.hullBoneTransform.position + orbitOffset;
        
        // Get the Camera Direction
        Vector3 dirToCamera = new Vector3(orbitOffset.x, 0, orbitOffset.z).normalized;
        
        // Set look ahead Dist based on Mouse Y delta, clamped
        lookAheadDistance = Mathf.Clamp(lookAheadDistance + vehicle.inputManager.MouseYDelta, 0, lookAheadMax);
        
        // Set position for camera to look at 
        Vector3 lookPoint = vehicle.hullBoneTransform.position + -dirToCamera * lookAheadDistance;
        transform.LookAt(lookPoint);
    }

    /// <summary>
    /// Shakes the camera with specified parameters
    /// </summary>
    public void Shake(ShakeParameters shakeParams)
    {
        if (shakeInstance != null && !shakeInstance.IsFinished)
            return;
        
        shakeInstance = shaker.Shake(shakeParams, UnityEngine.Random.Range(-10000, 10000));
    }

    // private void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawLine(this.transform.position, this.transform.position + this.transform.forward * 100F);
    //
    //     if (targetTransform == null)
    //         return;
    //     
    //     if (offset.x is > 0 or < 0)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.right * offset.x);
    //     }
    //
    //     if (offset.y is > 0 or < 0)
    //     {
    //         Gizmos.color = Color.green;
    //         Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.up * offset.y);
    //     }
    //
    //     if (offset.z is > 0 or < 0)
    //     {
    //         Gizmos.color = Color.blue;
    //         Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.forward * offset.z);
    //     }
    // }
    public void Enable()
    {
        OnProcessCamera += ProcessCamera;
    }
    
    public void Disable()
    {
        OnProcessCamera -= ProcessCamera;
    }
}
