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

public class CameraController : VehicleComponent
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
        
        // Add Camera functionality
        OnProcessCamera += () =>
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
        };
    }

    private void LateUpdate()
    {
        OnProcessCamera?.Invoke();
    }

    #region Old Camera Functionality V2

    // private List<double> ms1 = new List<double>();
    // private List<double> ms2 = new List<double>();
    // public int frameCount = 10;
    // private void ProcessCameraV2()
    // {
    //     // Camera needs to be in-line with tank and center of world and tank
    //     // Stopwatch stopwatch = Stopwatch.StartNew();
    //     // Vector3 basePos = targetTransform.position + Vector3.up * yOffset;
    //     // Vector3 dirToCenter = (worldCenter.position - targetTransform.position).normalized * -1;
    //     // Vector3 finalPos = basePos + dirToCenter * backOffset;
    //     // Vector3 lookTarget = Vector3.Lerp(targetTransform.position, worldCenter.position, LookAngleInterpolator);
    //     // transform.SetPositionAndRotation(finalPos, Quaternion.LookRotation(lookTarget - finalPos));
    //     // stopwatch.Stop();
    //     //
    //     // if (ms1.Count < frameCount)
    //     // {
    //     //     ms1.Add(stopwatch.Elapsed.TotalMilliseconds);
    //     // }
    //     //
    //     // if (ms1.Count == frameCount)
    //     // {
    //     //     OnGUISceneViewData.AddOrUpdateLabel($"Method 1: ", $"{stopwatch.Elapsed.TotalMilliseconds} ms");
    //     //     ms1.Clear();
    //     // }
    //
    //     // Stopwatch stopwatch2 = Stopwatch.StartNew();
    //     // this.transform.position = targetTransform.position + Vector3.up * yOffset;
    //     // this.transform.LookAt(worldCenter.position);
    //     // this.transform.position += transform.forward * backOffset;
    //     // this.transform.LookAt(Vector3.Lerp(targetTransform.position, worldCenter.position, LookAngleInterpolator));
    //     // stopwatch2.Stop();
    //     //
    //     // if (ms2.Count < frameCount)
    //     // {
    //     //     ms2.Add(stopwatch2.Elapsed.TotalMilliseconds);
    //     // }
    //     //
    //     // if (ms2.Count == frameCount)
    //     // {
    //     //     OnGUISceneViewData.AddOrUpdateLabel($"Method 2: ", $"{stopwatch2.Elapsed.TotalMilliseconds} ms");
    //     //     ms2.Clear();
    //     // }
    //     //
    //     // OnGUISceneViewData.AddOrUpdateLabel($"Cam Offset above tank: ", $"{Vector3.up * yOffset}");
    //     // OnGUISceneViewData.AddOrUpdateLabel($"Cam -Z offset: ", $"{this.transform.position.z}");
    // }
    #endregion
    
    #region Old Camera Functionality V1
    // public Vector3 shakeOffset;
    // private float interpolator = 0;
    // public float lerpSpeed = 1;
    // private float inputValue = 0;
    // public float debugInputValue = 0;
    // public Vector3 cachedPositionOffset;
    //
    // private void Awake()
    // {
    //     cachedPositionOffset = this.transform.position - targetTransform.position;
    // }
    // private void OldCam()
    // {
    //     // Set Position relative to target
    //     this.transform.position = targetTransform.position + cachedPositionOffset;
    //
    //     // Adjust offsetT towards value
    //     interpolator = Mathf.MoveTowards(interpolator, Mathf.Max(vehicle.inputManager.moveInput, debugInputValue), Time.deltaTime * lerpSpeed);
    //
    //     // Position is equal to the forward axis * offset (pos or neg)
    //     Vector3 lookAtPos = targetTransform.position + targetTransform.forward * Mathf.Max(interpolator, debugInputValue) * offset.z;
    //     lookAtPos += shakeOffset;
    //
    //     this.transform.LookAt(lookAtPos);
    //
    //     // Position our transform a distance away from the target on local Z
    //     this.transform.position = targetTransform.position - this.transform.forward * offset.y;
    // }
    #endregion

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
}
