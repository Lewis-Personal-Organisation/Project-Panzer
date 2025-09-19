using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : NetworkBehaviour
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
    public VehicleInputManager inputManager;
    
    [Header("Transforms")]
    [SerializeField] private Rigidbody hullRigidbody;
    [field: SerializeField] public Transform hullBoneTransform {get; private set;}
    [SerializeField] private Transform trackTransform;

    [Header("Color offset")]
    [Range(1, 12)]
    private int teamColor = 1;
    
    private float gravitationalForce;

    private enum InputState
    {
        None,
        Moving,
        Rotating,
        MovingAndRotating
    };
    private InputState inputState => (inputManager.rotationInput == 0, targetSpeed.IsNearZero()) switch
    {
        (true, true) => InputState.None,
        (true, false) => InputState.Moving,
        (false, false) => InputState.MovingAndRotating,
        (false, true) => InputState.Rotating
    };
    
    // Speed
    private float inputSpeed;
    private float targetSpeed => inputManager.moveInput switch
    {
        1 => mobility.forwardSpeed,
        -1 => -mobility.backwardSpeed,
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

    [Header("Force and Velocity")]
    private Vector3 localClampedVelocity;
    // private float preframeVelocityZ = 0;
    // [SerializeField] private float velocityZDelta = 0;
    // [SerializeField] int frameInterval = 5;
    public float localZVelocity { get; private set; }
    
    [Header("Ground Detection")]
    [SerializeField] private Transform leftCastPoint;
    [SerializeField] private Transform rightCastPoint;
    [SerializeField] private float castDistance = 0.03F;
    private bool isLeftGrounded = false;
    private bool isRightGrounded = false;
    private Vector3 lastFramePos;
    [SerializeField] private PhysicMaterial physicsMaterial;
    [SerializeField] private ArcDrawer arcDrawer;
    
    [Header("VFX")]
    [SerializeField] private VehicleVFXController vfxController;
    
    
    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        Debug.Log($"VehicleController :: Setup :: Are we host? {base.IsHost}");
        // viewCamera = Camera.main;

        if (!hullRigidbody)
        {
            if (TryGetComponent(out Rigidbody rb))
            {
                hullRigidbody = rb;
            }
            else
            {
                Debug.LogError("VehicleController :: Setup :: hullRigidbody not set", this.gameObject);
            }
        }

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
        if (!trackMaterial)
        {
            if (trackTransform.TryGetComponent(out Renderer renderer))
            {
                trackMaterial = renderer.material;
            }
            else
            {
                Debug.LogError("VehicleController :: Setup :: Track Material not set or found!", this.gameObject); 
            }
        }

        // Get and Set the materials ColorOffset
        paintMaterials = transform.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in paintMaterials)
        {
            renderer.material.SetFloat("_ColorOffset", (teamColor - 1));
        }

        if (!bodyLean)
        {
            if (TryGetComponent(out VehicleBodyLeanController leanController))
            {
                bodyLean = leanController;
            }
            else
            {
                Debug.LogError("VehicleController :: Setup :: BodyLeanController not set or found!", this.gameObject);
            }
        }
        
        if (!weaponLean)
        {
            if (TryGetComponent(out VehicleWeaponLeanController leanController))
            {
                weaponLean = leanController;
            }
            else
            {
                Debug.LogError("VehicleController :: Setup :: VehicleWeaponLeanController not set or found!", this.gameObject);
            }
        }

        ValueTracker exhaustSmoke = new(this,
            () => localClampedVelocity.z >= 10,
            () => localClampedVelocity.z < 10,
            () =>
            {
                vfxController.LerpLifetimeOptions(2, 0.2f);
                vfxController.EmitImmediate(50);
            });
    }

    // [Rpc(SendTo.Server)]
    // void SubmitPositionRequestRpc(RpcParams rpcParams = default)
    // {
    //     var randomPosition  = new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), 0, UnityEngine.Random.Range(-0.03f, 0.03f));
    //     hullRigidbody.MovePosition(spawnPosition + randomPosition);
    //     Position.Value = hullRigidbody.position;
    // }
    //
    // [Rpc(SendTo.Server)]
    // void SubmitRotationRequestRpc(RpcParams rpcParams = default)
    // {
    //     var randomPosition  = new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), 0, UnityEngine.Random.Range(-0.03f, 0.03f));
    //     hullRigidbody.MovePosition(spawnPosition + randomPosition);
    //     Position.Value = hullRigidbody.position;
    // }
    
    
    
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
            hullRigidbody.isKinematic = false;
        }
        
        RotateTank();

        UpdateTankMovementState();
        MoveTank();
            
        ApplyTrackScroll();
        
        bodyLean.UpdateLeanValues();
        hullBoneTransform.localRotation = Quaternion.Euler(bodyLean.LeanX + weaponLean.LeanX, 0, bodyLean.LeanZ + weaponLean.LeanZ);

        // cameraController.UpdateInputValue(moveInput);

        float speedAsT = Mathf.InverseLerp(mobility.forwardSpeed, 0, localZVelocity);
        vfxController.LerpLifetimeOptions(speedAsT, 0.2f);
    }
    
    private void RotateTank()
    {
        // If Grounded, allow rotation
        if (isLeftGrounded && isRightGrounded)
        {
            hullRigidbody.angularDrag = inputManager.rotationInput == 0 ? mobility.straightSteerDrag : mobility.steeringDrag;
            hullRigidbody.maxAngularVelocity = mobility.maxAngularVelocity;
            // Torque
            hullRigidbody.AddTorque(Vector3.up * inputManager.rotationInput * mobility.turnSpeed * Time.deltaTime * mobility.torqueMultipler, ForceMode.Force);
        }
        else
        {
            // else, apply an auto rotation
            // If rotation is higher than value, force rotation back
            float zAngle = this.transform.rotation.eulerAngles.z;
            if (Mathf.Abs(zAngle) > mobility.minAngleForCorrection)
            {
                Debug.Log($"Rotating with {(zAngle > 0 ? 1 : -1)}: {this.transform.rotation.eulerAngles.z}");
                hullRigidbody.AddTorque(hullBoneTransform.forward * mobility.correctionTorqueForce * (zAngle > 0 ? -1 : 1), ForceMode.Force);
            }
        }
    }

    private void UpdateTankMovementState()
    {
        // Check ground casts
        bool isLeftGroundedThisFrame = Physics.Raycast(leftCastPoint.position, leftCastPoint.forward, castDistance, LayerMask.GetMask("Ground", "PlayerAimDetectable"));
        bool isRightGroundedThisFrame = Physics.Raycast(rightCastPoint.position, rightCastPoint.forward, castDistance, LayerMask.GetMask("Ground", "PlayerAimDetectable"));
        
        // If state has not changed, return
        if (isLeftGroundedThisFrame == isLeftGrounded && isRightGroundedThisFrame == isRightGrounded) return;
        
        // Update cached data
        isLeftGrounded = isLeftGroundedThisFrame;
        isRightGrounded = isRightGroundedThisFrame;
        
        // If neither track has valid cast
        if (isLeftGrounded && isRightGrounded)
        {
            gravitationalForce = mobility.globalGravity;
            physicsMaterial.bounciness = mobility.physicsBounciness;
        }
        else if (!isLeftGrounded && !isRightGrounded)
        {
            // If we are still moving at or above a set distance, we are probably falling from some object
            if ((this.transform.position - lastFramePos).sqrMagnitude >= 0.5F)
            {
                gravitationalForce = mobility.localGravity;
            }
            else
            {
                bool tiltIsForwards = Vector3.SignedAngle(transform.up, Vector3.up, transform.right) > 0;
                Debug.Log($"Stuck!! {Vector3.SignedAngle(transform.forward, Vector3.forward, Vector3.up)}");
            }

            physicsMaterial.bounciness = 0;
            lastFramePos = hullRigidbody.position; // Cache for position comparison
        }
    }

    private void MeasureVelocityZDifference()
    {
        // float vel = transform.InverseTransformDirection(hullRigidbody.velocity).z;
        //
        // velocityFrames[1] = velocityFrames[0];
        // velocityFrames[0] = vel;
        //
        // if (Time.frameCount % 2 == 0)
        // {
        //     float total = 0;
        //     
        //     total = velocityFrames[0] + velocityFrames[1];
        //     total /= velocityFrames.Length;
        //
        //     velocityZDelta = total;
        // }
        
        // Debug.Log($"{transform.InverseTransformDirection(hullRigidbody.velocity).z * frameInterval}");
        // if (Time.frameCount % frameInterval == 0)
        // {
        //     float thisFrameVelocity = transform.InverseTransformDirection(hullRigidbody.velocity).z;
        //     
        //     velocityZDelta = thisFrameVelocity > preframeVelocityZ ? 1F : thisFrameVelocity < preframeVelocityZ ? -1F : 0F;
        //     preframeVelocityZ = thisFrameVelocity;
        // }
    }
    
    /// <summary>
    /// Moves the tank based on its grounded state, input values and gravity
    /// </summary>
    private void MoveTank()
    {
        // preframeVelocityZ = transform.InverseTransformDirection(hullRigidbody.velocity).z;
        
        if (isLeftGrounded || isRightGrounded)
        {
            hullRigidbody.drag = 1F;
            inputSpeed = Mathf.MoveTowards(inputSpeed, inputState != InputState.Rotating ? targetSpeed : mobility.steerVelocity, mobility.speedDelta * Time.deltaTime);
        }
        else
        {
            inputSpeed = Mathf.MoveTowards(inputSpeed, 0, mobility.speedDelta * Time.deltaTime);
        }
        
        // OnGUISceneViewData.forwardInputValue = inputManager.moveInput;
        // OnGUISceneViewData.inputSpeed = inputSpeed;
        // OnGUISceneViewData.speedTarget = isLeftGrounded || isRightGrounded ? targetSpeed : 0;

        if (arcDrawer)
            arcDrawer.startAngle = -Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), transform.forward, transform.right);

        float velocityDot = Vector3.Dot(hullRigidbody.velocity, hullRigidbody.transform.forward);
        
        // If Rotating, target speed more than allowed and velocity is higher than allowed
        if (inputState == InputState.Rotating && targetSpeed < mobility.steerVelocity && velocityDot > mobility.steerVelocity)
        {
            // FIX, DOESNT SEEM TO SCALE
            float velocityExcess = velocityDot - mobility.steerVelocity;
            float proportionalBrake = velocityExcess * mobility.brakeDelta;
            OnGUISceneViewData.brakeForce = proportionalBrake;
            hullRigidbody.AddRelativeForce(Vector3.back * proportionalBrake * Time.deltaTime, ForceMode.Acceleration);
        }
        else
        {
            hullRigidbody.AddRelativeForce(Vector3.forward * inputSpeed * mobility.forceMultiplier * Time.deltaTime, ForceMode.Force);
        }
        
        // Add Gravity
        hullRigidbody.AddForce(gravitationalForce * hullRigidbody.mass * Vector3.up);
        
        // Cache Velocity
        localClampedVelocity = transform.InverseTransformDirection(hullRigidbody.velocity);
        localClampedVelocity.x = 0;
        localZVelocity = localClampedVelocity.z;
        localClampedVelocity.z = Mathf.Clamp(localClampedVelocity.z, -mobility.backwardSpeed, mobility.forwardSpeed);
        hullRigidbody.velocity = transform.TransformDirection(localClampedVelocity);
        
        // OnGUISceneViewData.tankVelocity = hullRigidbody.velocity;
        // OnGUISceneViewData.tankLocalVelocity = localClampedVelocity;
    }

    /// <summary>
    /// Rotates the tank turret based on mouse position
    /// </summary>
    // private void RotateTankTurret()
    // {
    //     // Rotate turret
    //     targetPosition = hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
    //     targetPosition.y = 0f;
    //     Quaternion targetRotation = Quaternion.LookRotation(targetPosition);
    //     turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, targetRotation, Time.deltaTime * mobility.turretRotationSpeed);
    // }

    /// <summary>
    /// Sets the track material offset using velocity to mimi rotating tracks
    /// </summary>
    private void ApplyTrackScroll()
    {
        // Set track offset to match the lowest of velocity and track rotation.
        trackOffset += Mathf.Max(localClampedVelocity.z, inputManager.turnInputValue) * Time.deltaTime;
        trackOffset %= 1.0f;                                                            // track offset is always a remainder of 1.
        trackMaterial.SetFloat("_TrackOffset", trackOffset);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(leftCastPoint.position, leftCastPoint.forward * castDistance);
        Gizmos.DrawRay(rightCastPoint.position, rightCastPoint.forward * castDistance);
    }
}