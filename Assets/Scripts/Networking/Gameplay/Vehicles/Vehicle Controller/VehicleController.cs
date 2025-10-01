using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using InputStates = VehicleInputManager.InputState;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : NetworkVehicleComponent
{
    private class ValueTracker
    {
        private readonly MonoBehaviour host;
        private readonly Func<bool> activatePredicate;
        private readonly Func<bool> resetPredicate;
        private readonly UnityAction onValueReached = null;
        
        public ValueTracker(MonoBehaviour host, Func<bool> activatePredicate, Func<bool> resetPredicate, UnityAction onValueReached)
        {
            this.host = host;
            this.activatePredicate = activatePredicate;
            this.resetPredicate = resetPredicate;
            this.onValueReached = onValueReached;
            host.StartCoroutine(WaitForActivate());
        }
        
        private IEnumerator WaitForActivate()
        {
            yield return new WaitUntil(activatePredicate);
            onValueReached.Invoke();
            host.StartCoroutine(Reset());
        }
        
        private IEnumerator Reset()
        {
            yield return new WaitUntil(resetPredicate);
            host.StartCoroutine(WaitForActivate());
        }
    }
    
    [Header("Core Parameters")]
    public VehicleMobility mobility;
    [field: SerializeField] public CameraController cameraController {get; private set;}
    [field: SerializeField] public VehicleInputManager inputManager {get; private set;}
    [SerializeField] private RigidBodyVelocityTracker velocityTracker;
    [SerializeField] private VehicleGroundDetector groundDetector;
    [SerializeField] private VehicleVFXController vfxController;
    
    [Header("Transforms")]
    [SerializeField] private Rigidbody hullRigidbody;
    // [SerializeField] private BoxCollider hullCollider;
    [field: SerializeField] public Transform hullBoneTransform {get; private set;}
    [SerializeField] private Transform trackTransform;

    [Header("Color offset")]
    [Range(1, 12)]
    private int teamColor = 1;
    
    internal float gravitationalForce;
    
    // Speed
    private float inputSpeed;
    private float targetInputSpeed => inputManager.moveInput switch
    {
        1 => 1,
        -1 => -1,
        _ => 0
    };

    [Header("Tracks")]
    [SerializeField] private Material trackMaterial;
    [SerializeField] private float trackOffset = 0.0f;

    [Header("Lean Controllers")]
    public VehicleBodyLeanController bodyLean;
    public VehicleWeaponLeanController weaponLean;
    
    [Header("Team Colour")]
    private Renderer[] paintMaterials;
    
    
    private void Awake()
    {
        Setup();
    }
    
    public void Setup()
    {
        Debug.Log($"VehicleController :: Setup :: Are we host? {base.IsHost}");
        // viewCamera = Camera.main;

        TryGetComponent(ref hullRigidbody);
        TryGetComponent(ref bodyLean);
        TryGetComponent(ref weaponLean);

        if (trackTransform.TryGetComponent<Renderer>(out Renderer rend) && rend)
        {
            trackMaterial = rend.material;
        }
        else
        {
            Debug.LogError("VehicleController :: Setup :: Track Material not set or found!", this.gameObject);
        }
        
        paintMaterials = transform.GetComponentsInChildren<Renderer>();

        if (!mobility)
        {
            Debug.LogError("VehicleController :: Setup :: Mobility field not set", this.gameObject);
        }
        
        gravitationalForce = mobility.localGravity;
        
        // RE-ENABLE FOR NETWORKING
        // if (IsOwner)
        // {
            // this.gameObject.layer = LayerMask.NameToLayer("PlayerSelf");
        // }

        foreach (Renderer r in paintMaterials)
        {
            r.material.SetFloat("_ColorOffset", (teamColor - 1));
        }

        ValueTracker exhaustSmoke = new ValueTracker(this,
            () => velocityTracker.z.velocity >= 10, 
            () => velocityTracker.z.velocity < 10, () =>
            {
                vfxController.LerpLifetimeOptions(2, 0.2f);
                vfxController.EmitImmediate(50);
            });
    }
    
    // [BurstCompile]
    private void FixedUpdate()
    {
        if (NetworkManager != null)
        {
            if (!IsOwner)
                return;
        }
        else
        {
            // FIX FOR KINEMATIC BEING SET TRUE BY UNITY NETWORK
            // Should be optimized
            hullRigidbody.isKinematic = false;
        }
        
        groundDetector.DetectGroundState();
        RotateTank();
        MoveTank();
        
        ApplyTrackScroll();
        
        bodyLean.UpdateLeanValues();
        hullBoneTransform.localRotation = Quaternion.Euler(bodyLean.LeanX + weaponLean.LeanX, 0, bodyLean.LeanZ + weaponLean.LeanZ);

        

        float speedAsT = Mathf.InverseLerp(mobility.forwardSpeed, 0, velocityTracker.z.velocity);
        vfxController.LerpLifetimeOptions(speedAsT, 0.2f);
    }
    
    private void RotateTank()
    {
        // If Grounded, allow input rotation
        if (groundDetector.leftSideIsGrounded && groundDetector.rightSideIsGrounded)
        {
            hullRigidbody.angularDrag = inputManager.rotationInput == 0 ? mobility.straightSteerDrag : mobility.steeringDrag;
            hullRigidbody.maxAngularVelocity = mobility.maxAngularVelocity;
            // Torque
            hullRigidbody.AddTorque(Vector3.up * inputManager.rotationInput * mobility.turnSpeed * Time.deltaTime * mobility.torqueMultipler, ForceMode.Force);
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
    
    /// <summary>
    /// Moves the tank based on its grounded state, input values and gravity
    /// </summary>
    private void MoveTank()
    {
        bool clampToSteerVelocity = false;
        if (groundDetector.leftSideIsGrounded || groundDetector.rightSideIsGrounded)
        {
            OnGUISceneViewData.AddOrUpdateLabel("Input State", $"{inputManager.inputState}", Color.black);
            
            switch (inputManager.inputState)
            {
                case InputStates.MovingForward or InputStates.MovingForwardAndRotating:
                case InputStates.MovingBackward or InputStates.MovingBackwardAndRotating:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, targetInputSpeed, mobility.speedDelta * Time.deltaTime);
                    break;

                case InputStates.Rotating:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, 1, mobility.speedDelta * Time.deltaTime);
                    break;
                
                case InputStates.None:
                    inputSpeed = Mathf.MoveTowards(inputSpeed, 0, mobility.brakeDelta * Time.deltaTime);
                    break;
            }
        }
        else
        {
            inputSpeed = Mathf.MoveTowards(inputSpeed, 0, mobility.speedDelta * Time.deltaTime);
        }
        
        OnGUISceneViewData.AddOrUpdateLabel("Clamped Input", $"{clampToSteerVelocity}", Color.black);
        OnGUISceneViewData.AddOrUpdateLabel("Input Speed: ", $"{inputSpeed}", Color.black);
        
        // If Rotating, target speed more than allowed and velocity is higher than allowed
        if (inputManager.inputState == InputStates.Rotating && velocityTracker.z.velocity > mobility.steerVelocity)
        {
            // FIX, DOESNT SEEM TO SCALE
            // float velocityExcess = velocityTracker.z.velocity - mobility.steerVelocity;
            // float proportionalBrake = velocityExcess * mobility.brakeDelta;
            // hullRigidbody.AddRelativeForce(Vector3.back * proportionalBrake * Time.deltaTime, ForceMode.Acceleration);
        }
        else
        {
            hullRigidbody.AddRelativeForce(Vector3.forward * inputSpeed * mobility.forceMultiplier * Time.deltaTime, ForceMode.Force);
            OnGUISceneViewData.AddOrUpdateLabel("Moving Forward Only Force: ", $"{Vector3.forward * inputSpeed * mobility.forceMultiplier * Time.deltaTime}", Color.black);
        }
        
        // Add Gravity
        hullRigidbody.AddForce(gravitationalForce * hullRigidbody.mass * Vector3.up);
        
        // Set Velocity
        float zMin = clampToSteerVelocity ? -mobility.steerVelocity : -mobility.backwardSpeed;
        float zMax = clampToSteerVelocity ? mobility.steerVelocity : mobility.forwardSpeed;
        hullRigidbody.velocity = transform.TransformDirection(new Vector3(0, velocityTracker.y.velocity, velocityTracker.z.Clamped(zMin, zMax)));
        OnGUISceneViewData.AddOrUpdateLabel("Velocity: ", $"{velocityTracker.z.Clamped(zMin, zMax)}", Color.black);
        
        inputManager.SetLastInputState();
    }

    /// <summary>
    /// Sets the track material offset using velocity to mimi rotating tracks
    /// </summary>
    private void ApplyTrackScroll()
    {
        // Set track offset to match the lowest of velocity and track rotation.
        trackOffset += Mathf.Max(velocityTracker.z.velocity, inputManager.turnInputValue) * Time.deltaTime;
        trackOffset %= 1.0f;                                                            // track offset is always a remainder of 1.
        trackMaterial.SetFloat("_TrackOffset", trackOffset);
    }
}