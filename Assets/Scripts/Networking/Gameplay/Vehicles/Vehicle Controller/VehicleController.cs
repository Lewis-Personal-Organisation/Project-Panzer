using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : NetworkVehicleComponent, IVehicleComponentToggleable
{
    public static VehicleController Instance { get; private set; }
    public VehicleWeaponController WeaponController => weaponController;

    [Header("Core Components")]
    public VehicleMobility mobility;
    [field: SerializeField] public CameraController cameraController {get; private set;}
    [field: SerializeField] public VehicleInputManager inputManager {get; private set;}
    [field: SerializeField] public RigidBodyVelocityTracker velocityTracker {get; private set;}
    public VehicleGroundDetector groundDetector;
    public VehicleBodyMover bodyMover;
    [SerializeField] private VehicleBodyRotator bodyRotator;
    [SerializeField] private VehicleTurretRotator turretRotator;
    [SerializeField] private VehicleVFXController vfxController;
    [SerializeField] private VehicleTrackTextureScroller trackTextureScroller;
    [SerializeField] private VehicleWeaponController weaponController;
    public VehicleDefence defence;
    public VehicleStuckManager stuckManager;
    [SerializeField] private AudioListener audioListener;
    
    [Header("Transforms")]
    public Rigidbody hullRigidbody;
    [field: SerializeField] public Transform hullBoneTransform {get; private set;}
    public Transform centerTransform;
    
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

    [DisableIf("@!EditorApplication.isPlaying")]
    [Button(ButtonSizes.Medium), GUIColor(0.271F, 0.271F, 0.929F)]
    private void Revive()
    {
        inputManager.enabled = true;
        weaponController.Enable();
        turretRotator.Enable();
        defence.Enable();
        vfxController.aliveParticles.Value = true;
    }
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Setup();
            // Debug.Log($"Player Object is {NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.name}", gameObject);
            // Debug.Log($"VehicleController :: OnNetworkSpawn() :: IsLocalPlayer => {playerAvatar.IsLocalPlayer} (playerAvatar)", this.gameObject);
        }
        else
        {
            inputManager.enabled = false;
            velocityTracker.enabled = false;
            weaponController.enabled = false;
            turretRotator.enabled = false;
            defence.enabled = false;
            stuckManager.enabled = false;
        }
    }

    private void Awake()
    {
        if (testing && !IsNetworked)
        {
            Setup();
            inputManager.enabled = true;
        }
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
        Instance = this;
        
        Debug.Log($"VehicleController :: Setup :: Called! We are the owner", this.gameObject);

        if (!mobility)
        {
            Debug.LogError("VehicleController :: Setup :: Mobility field not set", this.gameObject);
        }
        
        cameraController = FindObjectOfType<CameraController>();
        cameraController.Setup(this);
        
        weaponController.Setup(this);
        turretRotator.Setup(this);
        defence.Setup(this);
        vfxController.Setup(this);
        stuckManager.Setup(this);

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
    
    public void DisableSoft()
    {
        inputManager.enabled = false;
        weaponController.Disable();
        // turretRotator.Disable();
        cameraController.takeOrbitInput = false;
    }
    
    public void EnableSoft()
    {
        inputManager.enabled = true;
        weaponController.Enable();
        // turretRotator.Enable();
        cameraController.takeOrbitInput = true;
    }

    /// <summary>
    /// Called when the player dies - stop systems
    /// </summary>
    public void Disable()
    {
        inputManager.enabled = false;
        weaponController.Disable();
        turretRotator.Disable();
        defence.Disable();
    }

    public void Destroy()
    {
        Debug.Log("PLAYER DESTROYED");
        Disable();
        vfxController.aliveParticles.Value = false;
    }

    public void Respawn()
    {
        Debug.Log("PLAYER RESPAWNED");
        vfxController.OnRespawn();
    }

    // Called when the player respawns. Reactivates systems
    public void Enable()
    {
        Debug.Log("PLAYER ENABLED");
        inputManager.enabled = true;
        weaponController.Enable();
        turretRotator.Enable();
        defence.Enable();
    }
}