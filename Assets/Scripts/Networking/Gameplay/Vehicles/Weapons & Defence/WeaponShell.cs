using System;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode.Components;
using UnityEngine;

/*  This class is attached to a moving project or 'Shell'
 *  It is responsible for moving the projectile on every client
 *  Whether Authoritive or not
 */

public class WeaponShell : WeaponAmmoBehaviour, IDebuggable
{
    [SerializeField] private Rigidbody rigidBody;
	
    [Header("Movement")]
    [SerializeField] private float velocity;

    private Action OnNetworkUpdate;
    private Action OnNetworkFixedUpdate;

    [Header("Debugging")]
    [SerializeField] private bool debugMode;
    public bool DebugMode { get => debugMode; set => debugMode = value; }
    [ShowIf("debugMode", true)] public bool isTraversing = false;
    [ShowIf("debugMode", true)] public Vector3 startPos;
    private CancellationTokenSource _cts;
    [ShowIf("debugMode", true)]
    [DisableIf("@!debugMode || !EditorApplication.isPlaying || isTraversing")]
    [Button(ButtonSizes.Medium), GUIColor(0.271F, 0.271F, 0.929F)]
    private async void Traverse()
    {
        if (startPos != Vector3.zero)
            startPos = this.transform.position;
        
        isTraversing = true;
        _cts = new CancellationTokenSource();
        float t = 0;

        try
        {
            while (isTraversing && t < 5f)
            {
                float dt = EditorApplicationUpdater.DeltaTime;
                rigidBody.MovePosition(rigidBody.position + transform.forward * dt * velocity);
                t += dt;
                await Task.Yield();
            }
        }
        finally
        {
            isTraversing = false;
        }
    }

    private void Awake()
    {
        if (!networkTransform)
            networkTransform = GetComponent<NetworkTransform>();

        trailTime = trailRenderer.time;
    }

    // Subscribe when spawned and execute Data change to sync network state
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        spawnData.OnValueChanged += OnSpawnDataChanged;
        
        OnSpawnDataChanged(spawnData.Value, spawnData.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        spawnData.OnValueChanged -= OnSpawnDataChanged;
    }
    
    private void OnSpawnDataChanged(ShellSpawnData oldData, ShellSpawnData newData)
    {
        if (newData.Pooled)
            return;
        
        // Setup shell if it's owned by this client
        if (IsOwner)
        {
            Debug.Log($"We now own Shell {transform.name}. It was {(newData.Pooled ? "pooled" : "fired")}", gameObject);
            networkTransform.Teleport(newData.Position, newData.Rotation, transform.localScale);
                
            lifetimeTimer = lifetime;
            
            OnNetworkUpdate = OwnerNetworkUpdate;
            OnNetworkFixedUpdate = NetworkedFixedUpdate;
            
            ToggleVisuals(true);
        }
        
        // Clear Trail so it doesn't stretch from Pooled to new location, for all clients
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }
    }

    /// <summary>
    /// Called by the server or Locally in non-networked scenarios
    /// </summary>
	public override void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        lifetimeTimer = lifetime;
    }
    
    private void Update()
    {
        if (VehicleController.IsNetworked)
        {
            OnNetworkUpdate?.Invoke();
        }
        else
        {
            OnLocalUpdate();     // The Local-only/Testing Update
        }
    }
    
    /// <summary>
    /// The update method called when connected to a network
    /// </summary>
    public override void OwnerNetworkUpdate()
    {
        if (spawnData.Value.Pooled) return;
        if (!IsOwner) return;

        // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            VehicleController.Instance.WeaponController.ReturnToPoolServerRpc(NetworkObject);
        }
    }

    /// <summary>
    /// The Update method called when not connected to a network
    /// </summary>
    public override void OnLocalUpdate()
    {
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// Moves the shell guide and visuals.
    /// Visual shell is Rotated towards new rotation 
    /// </summary>
    private void FixedUpdate()
    {
        if (VehicleController.IsNetworked)
        {
            OnNetworkFixedUpdate?.Invoke();
        }
        else
        {
            OnLocalFixedUpdate();
        }
    }

    /// <summary>
    /// Called when the Network is active
    /// </summary>
    public override void NetworkedFixedUpdate()
    {
        if (spawnData.Value.Pooled)
        {
            return; // If pooled (only spawnable)
        }

        // Move only if we own this object - Network Transform synchronises to every client!
        if (IsOwner)
        {
            rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
        }
    }

    /// <summary>
    /// Called when in solo play
    /// </summary>
    public override void OnLocalFixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
    }
}