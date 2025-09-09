using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class TankController : NetworkBehaviour
{
    [Serializable]
    public class VehicleLean
    {
        private readonly TankController controller;
        
        private float xWeaponLean;
        private float zWeaponLean;
        
        private float xLean = 0.0f;
        private float xLeanTimer = 0F;
        private float xTargetLean = 0;
        private const float XLeanTimerMax = 0.5F;
        
        private float zLean = 0.0f;

        public VehicleLean(TankController controller) => this.controller = controller;
        private TankMobility data => controller.data;
        private Transform hullTransform => controller.hullBoneTransform;
        
        internal float tiltSign => controller.localZVelocity > data.minForwardVelocity ? -1F : controller.localZVelocity < -data.minForwardVelocity ? 1F : 0F;

        /// <summary>
        /// Applies the Horizontal and Vertical lean based on velocity, direction and gun fire
        /// </summary>
        internal void Lean()
        {
            // Hull Lean X
            float leanMode = xLeanTimer < XLeanTimerMax ? data.verticalMaxLean : data.verticalRestingLean;  // Vertical or resting lean value
            xTargetLean = leanMode * tiltSign;                                                              // Should the value be pos or neg? (Moving forwards should lean backward etc.)
            xLean = Mathf.Lerp(xLean, xTargetLean, Time.deltaTime * data.verticalLeanSpeed);                // Move value used for the lean rotation
            
            //If current lean is above target (minus a subtracted value), increment timer. Else, reset timer
            if (Mathf.Abs(xLean) > Mathf.Abs(xTargetLean) - data.verticalToRestingLeanValue)
            {
                xLeanTimer = Mathf.Clamp(xLeanTimer + Time.deltaTime, 0, XLeanTimerMax);
            }
            else
            {
                xLeanTimer = 0;
            }

            // Hull lean Z
            zLean = Mathf.Lerp(zLean, controller.turnInputValue * data.horizontalMaxLean, Time.deltaTime * data.horizontalLeanSpeed);
            
            // Apply Lean
            hullTransform.localRotation = Quaternion.Euler(xLean + xWeaponLean, 0, zLean + zWeaponLean);
        }

        /// <summary>
        /// Called externally. Provides the lean from firing weapon
        /// </summary>
        public void UpdateWeaponLean(float leanX, float leanZ)
        {
            xWeaponLean = leanX;
            zWeaponLean = leanZ;
        }
    }
    
    [SerializeField] private CameraController cameraController;

    [field: SerializeField] public Transform hullBoneTransform {get; private set;}
    [SerializeField] private Rigidbody hullRigidbody;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform trackTransform;
    [SerializeField] private Transform targetTransform;

    [Header("Color offset")]
    [Range(1, 12)]
    private int teamColor = 1;
    
    [Header("Tank Parameters")]
    [SerializeField] public VehicleType type;
    [SerializeField] private TankMobility data;

    [SerializeField] private Material trackMaterial;
    private float gravitationalForce;

    private enum InputState
    {
        None,
        Moving,
        Rotating,
        MovingAndRotating
    };
    private InputState inputState => (yRotationDelta.IsNearZero(), targetSpeed.IsNearZero()) switch
    {
        (true, true) => InputState.None,
        (true, false) => InputState.Moving,
        (false, false) => InputState.MovingAndRotating,
        (false, true) => InputState.Rotating
    };
    
    [Header("Forward Input and Speed")]
    private float inputSpeed;
    private float moveInput = 0F;
    private float targetSpeed => moveInput switch
    {
        1 => data.forwardSpeed,
        -1 => -data.backwardSpeed,
        _ => 0
    };
    private float turnInputValue = 0.0f;
    private Vector3 targetPosition;
    private float yRotationDelta;

    [Header("Tracks Offset and Hull lean")]
    private float trackOffset = 0.0f;
    
    [field: HideInInspector] public VehicleLean leanManager { get; private set; }
    
    [Header("Team Colour")]
    private Renderer[] paintMaterials;

    [Header("Force and Velocity")]
    private Vector3 localClampedVelocity;
    private float localZVelocity;
    
    [Header("Casting and Movement Delta")]
    [SerializeField] private Transform leftCastPoint;
    [SerializeField] private Transform rightCastPoint;
    [SerializeField] private float castDistance = 0.03F;
    private bool isGrounded => isLeftGrounded && isRightGrounded;
    private bool isPartiallyGrounded => isLeftGrounded || isRightGrounded;
    private bool isLeftGrounded = false;
    private bool isRightGrounded = false;
    private Vector3 lastFramePos;
    [SerializeField] private PhysicMaterial physicsMaterial;
    [SerializeField] private ArcDrawer arcDrawer;
    
    [Header("VFX")]
    [SerializeField] private TankVFXController vfxController;
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
    
    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        Debug.Log($"TankController :: Setup :: Are we host? {base.IsHost}");
        // viewCamera = Camera.main;

        if (!hullRigidbody)
            hullRigidbody = GetComponent<Rigidbody>();
        
        gravitationalForce = data.localGravity;
        
        // RE-ENABLE FOR NETWORKING
        // if (IsOwner)
        // {
            // this.gameObject.layer = LayerMask.NameToLayer("PlayerSelf");
        // }

        trackMaterial = trackTransform.GetComponent<Renderer>().material;

        // Get and Set the materials ColorOffset
        paintMaterials = transform.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in paintMaterials)
        {
            renderer.material.SetFloat("_ColorOffset", (teamColor - 1));
        }

        leanManager = new VehicleLean(this);

        ValueTracker exhaustPoof = new(this,
            () => localClampedVelocity.z >= 10,
            () => localClampedVelocity.z < 10,
            () =>
            {
                vfxController.LerpLifetimeOptions(2, 0.2f);
                vfxController.EmitImmediate(50);
            });
    }

    // Update is called once per frame
    void Update()
    {
        AdjustMouseAimPosition();
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
        
        // Get input values for movement
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");
        
        RotateTank();

        UpdateTankMovementState();
        MoveTank();
        ApplyTrackScroll();
        leanManager.Lean();
        
        RotateTankTurret();

        cameraController.UpdateInputValue(moveInput);

        float speedAsT = Mathf.InverseLerp(data.forwardSpeed, 0, localZVelocity);
        vfxController.LerpLifetimeOptions(speedAsT, 0.2f);
    }

    private void AdjustMouseAimPosition()
    {
        if (!Physics.Raycast(cameraController.camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground"))) return;
        
        targetPosition = hit.point;
            
        // move the target object to the hit position
        targetTransform.position = targetPosition;
    }
    
    private void RotateTank()
    {
        // REPLACE WITH TORQUE
        yRotationDelta = turnInputValue * data.turnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, yRotationDelta, 0f);
        hullRigidbody.MoveRotation(hullRigidbody.rotation * turnRotation);
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
        if (!isGrounded)
        {
            // If we are still moving at or above a set distance, we are probably falling from some object
            if ((this.transform.position - lastFramePos).sqrMagnitude >= 0.5F)
            {
                gravitationalForce = data.localGravity;
            }
            else
            {
                bool tiltIsForwards = Vector3.SignedAngle(transform.up, Vector3.up, transform.right) > 0;
                Debug.Log($"Stuck!! {Vector3.SignedAngle(transform.forward, Vector3.forward, Vector3.up)}");
            }

            physicsMaterial.bounciness = 0;
            lastFramePos = hullRigidbody.position; // Cache for position comparison
        }
        else
        {
            gravitationalForce = data.globalGravity;
            physicsMaterial.bounciness = 0.375F;
        }
    }

    /// <summary>
    /// Moves the tank based on its grounded state, input values and gravity
    /// </summary>
    private void MoveTank()
    {
        if (isPartiallyGrounded)
        {
            hullRigidbody.drag = inputState == InputState.Rotating ? data.steerDrag : data.maxDrag;
            inputSpeed = Mathf.MoveTowards(inputSpeed, inputState != InputState.Rotating ? targetSpeed : data.steerVelocity, data.speedDelta * Time.deltaTime);
        }
        
        OnGUISceneViewData.forwardInputValue = moveInput;
        OnGUISceneViewData.inputSpeed = inputSpeed;
        OnGUISceneViewData.speedTarget = isPartiallyGrounded ? targetSpeed : 0;

        arcDrawer.startAngle = -Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), transform.forward, transform.right);

        float velocityDot = Vector3.Dot(hullRigidbody.velocity, hullRigidbody.transform.forward);
        
        // If Rotating, target speed more than allowed and velocity is higher than allowed
        if (inputState == InputState.Rotating && targetSpeed < data.steerVelocity && velocityDot > data.steerVelocity)
        {
            // FIX, DOESNT SEEM TO SCALE
            float velocityExcess = velocityDot - data.steerVelocity;
            float proportionalBrake = velocityExcess * data.brakeDelta;
            OnGUISceneViewData.brakeForce = proportionalBrake;
            hullRigidbody.AddRelativeForce(Vector3.back * proportionalBrake * Time.deltaTime, ForceMode.Acceleration);
        }
        else
        {
            hullRigidbody.AddRelativeForce(Vector3.forward * inputSpeed * data.forceMultiplier * Time.deltaTime, ForceMode.Force);
        }
        
        localClampedVelocity = transform.InverseTransformDirection(hullRigidbody.velocity);
        localClampedVelocity.x = 0;
        localZVelocity = localClampedVelocity.z;
        localClampedVelocity.z = Mathf.Clamp(localClampedVelocity.z, -data.backwardSpeed, data.forwardSpeed);
        hullRigidbody.velocity = transform.TransformDirection(localClampedVelocity);

        // Add Gravity
        hullRigidbody.AddForce(gravitationalForce * hullRigidbody.mass * Vector3.up);
        
        OnGUISceneViewData.tankVelocity = hullRigidbody.velocity;
        OnGUISceneViewData.tankLocalVelocity = localClampedVelocity;
        OnGUISceneViewData.rotationDelta = yRotationDelta;
    }

    /// <summary>
    /// Rotates the tank turret based on mouse position
    /// </summary>
    private void RotateTankTurret()
    {
        // Rotate turret
        targetPosition = hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
        targetPosition.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition);
        turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, targetRotation, Time.deltaTime * data.turretSpeed);
    }

    /// <summary>
    /// Sets the track material offset using velocity to mimi rotating tracks
    /// </summary>
    private void ApplyTrackScroll()
    {
        // Set track offset to match the lowest of velocity and track rotation.
        trackOffset += Mathf.Max(localClampedVelocity.z, turnInputValue) * Time.deltaTime;
        trackOffset %= 1.0f;                                                            // track offset is always a remainder of 1.
        trackMaterial.SetFloat("_TrackOffset", trackOffset);
    }
    
    
        
        // // Lean the hull based on movement inputs
        // if (Input.GetButtonDown("Vertical"))
        // {
        //     isForward = Input.GetAxisRaw("Vertical");
        //     targetHullForwardLean = -data.verticalMaxLean * isForward;
        // }
        //
        // if (Input.GetButtonUp("Vertical"))
        // {
        //     targetHullForwardLean = data.verticalMaxLean * isForward;
        // }
        //
        // // Hull lean
        // // targetHullLean = -turnInputValue * data.horizontalMaxLean;
        // // actHullLean = Mathf.Lerp(actHullLean, targetHullLean, Time.deltaTime * data.horizontalLeanSpeed);
        // zCurrentLean = Mathf.Lerp(zCurrentLean, targetHullForwardLean, Time.deltaTime * data.verticalLeanSpeed);
        //
        // if (Mathf.Abs(zCurrentLean) >= Mathf.Abs(targetHullForwardLean) - 1.0f)
        // {
        //     targetHullForwardLean = moveInput > 0 ? restingHullVertLean * -1 : 0;
        // }
        //
        // hullBoneTransform.localRotation = Quaternion.Euler(xLean * moveInput, 0, zCurrentLean);
    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(leftCastPoint.position, leftCastPoint.forward * castDistance);
        Gizmos.DrawRay(rightCastPoint.position, rightCastPoint.forward * castDistance);
    }
}