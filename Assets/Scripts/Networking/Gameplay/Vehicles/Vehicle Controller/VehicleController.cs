using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : NetworkVehicleComponent, IVehicleComponentToggleable
{
    [Header("Core Components")]
    public VehicleMobility mobility;
    [field: SerializeField] public CameraController cameraController {get; private set;}
    [field: SerializeField] public VehicleInputManager inputManager {get; private set;}
    [field: SerializeField] public RigidBodyVelocityTracker velocityTracker {get; private set;}
    public VehicleGroundDetector groundDetector;
    [SerializeField] private VehicleBodyMover bodyMover;
    [SerializeField] private VehicleBodyRotator bodyRotator;
    [SerializeField] private VehicleTurretRotator turretRotator;
    [SerializeField] private VehicleVFXController vfxController;
    [SerializeField] private VehicleTrackTextureScroller trackTextureScroller;
    [SerializeField] private VehicleWeaponController weaponController;
    [SerializeField] private VehicleDefence defence;
    [SerializeField] private VehicleStuckManager stuckManager;
    [SerializeField] private AudioListener audioListener;
    
    [Header("Transforms")]
    public Rigidbody hullRigidbody;
    [field: SerializeField] public Transform hullBoneTransform {get; private set;}
    
    [Header("Color offset")]
    [Range(1, 12)]
    private int teamColor = 1;
    
    internal float gravitationalForce;
    
    [Header("Lean Controllers")]
    public VehicleBodyLeanController bodyLean;
    public VehicleWeaponLeanController weaponLean;
    
    [Header("Team Colour")]
    private Renderer[] paintMaterials;

    public static bool IsNetworked => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    
    public bool testing = false;

    private UnityAction OnFixedUpdate = null;
    
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Setup();
            // Debug.Log($"Player Object is {NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.name}", gameObject);
            // Debug.Log($"VehicleController :: OnNetworkSpawn() :: IsLocalPlayer => {playerAvatar.IsLocalPlayer} (playerAvatar)", this.gameObject);
        }
    }

    private void Awake()
    {
        if (testing && !IsNetworked)
            Setup();
    }
    
    // [BurstCompile]
    private void FixedUpdate()
    {
        OnFixedUpdate?.Invoke();
    }

    /// <summary>
    /// The setup called on only the owning players client
    /// </summary>
    private void Setup()
    {
        Debug.Log($"VehicleController :: Setup :: Called! We are the owner", this.gameObject);

        if (!mobility)
        {
            Debug.LogError("VehicleController :: Setup :: Mobility field not set", this.gameObject);
        }
        
        cameraController = FindObjectOfType<CameraController>();
        cameraController.Setup(this);
        
        TryGetComponent(ref hullRigidbody);
        TryGetComponent(ref bodyRotator);
        TryGetComponent(ref bodyLean);
        TryGetComponent(ref weaponLean);
        TryGetComponent(ref audioListener);
        TryGetComponentAdv(ref weaponController, false)?.Setup(this);
        TryGetComponentAdv(ref turretRotator)?.Setup(this);
        TryGetComponentAdv(ref defence)?.Setup(this);
        TryGetComponentAdv(ref vfxController)?.Setup(this);
        TryGetComponentAdv(ref stuckManager)?.Setup(this);

        audioListener.enabled = true;

        paintMaterials = transform.GetComponentsInChildren<Renderer>();

        bodyMover.RetainedVelocity = mobility.traction;
        gravitationalForce = mobility.localGravity;

        foreach (Renderer r in paintMaterials)
        {
            r.material.SetFloat("_ColorOffset", teamColor - 1);
        }

        OnFixedUpdate += ProcessVehicle;
    }

    /// <summary>
    /// Process Vehicle components
    /// </summary>
    private void ProcessVehicle()
    {
        if (NetworkManager == null && !testing)
            Debug.LogError("VehicleController :: NetworkManager not set!");
        
        if (!IsOwner && !testing)
            return;
        
        // FIX FOR KINEMATIC BEING SET TRUE BY UNITY NETWORK
        // Should be optimized
        if (hullRigidbody.isKinematic)
        {
            hullRigidbody.isKinematic = false;
            Debug.Log("VehicleController :: hullRigidbody.isKinematic corrected!");
        }
        
        groundDetector.DetectGroundState();

        // SceneData.Label("Can Rotate? ", $"{groundDetector.FullyGrounded}");
        if (groundDetector.FullyGrounded)
        {
            bodyRotator.RotateTank();
        }
        
        bodyMover.MoveTank(groundDetector.PartiallyGrounded);
        
        trackTextureScroller.ApplyTrackScroll();
        
        bodyLean.UpdateLeanValues();
        hullBoneTransform.localRotation = Quaternion.Euler(bodyLean.LeanX + weaponLean.LeanX, 0, bodyLean.LeanZ + weaponLean.LeanZ);;
        
        float speedAsT = Mathf.InverseLerp(mobility.forwardSpeed, 0, velocityTracker.z.velocity);
        vfxController.LerpLifetimeOptions(speedAsT, 0.2f);
    }

    /// <summary>
    /// Called when the player dies - stop systems
    /// </summary>
    public void Disable()
    {
        Debug.Log("PLAYER DESTROYED");
        inputManager.enabled = false;
        weaponController.Disable();
        turretRotator.Disable();
        defence.Disable();
        vfxController.aliveParticles.Value = false;
    }

    // Called when the player respawns. Reactivates systems
    public void Enable()
    {
        vfxController.OnRespawn();
    }
}