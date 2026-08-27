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
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime;
    private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Movement")]
    [SerializeField] private float velocity;
    private float shellSpeed;

    private Action OnOwnerNetworkUpdate;
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
                await Task.Yield(); // or await Task.Delay(...) — stays cooperative, no thread switch
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
    }
    
    private void OnEnable()  => spawnData.OnValueChanged += OnSpawnDataChanged;
    private void OnDisable() => spawnData.OnValueChanged -= OnSpawnDataChanged;
    private void OnSpawnDataChanged(ShellSpawnData pre, ShellSpawnData curr)
    {
        TryApplySpawnData(curr);
    }

    /// <summary>
    /// Called by the server or Locally in non-networked scenarios
    /// </summary>
	public override void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        shellSpeed = velocity;
        lifetimeTimer = lifetime;
    }

    private bool pendingTeleport = false;
    
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        base.OnOwnershipChanged(previous, current);
        
        if (!IsOwner)
            return;
        
        lifetimeTimer = lifetime;
        shellSpeed = velocity;
        
        OnOwnerNetworkUpdate = OwnerNetworkUpdate;
        OnNetworkFixedUpdate = NetworkedFixedUpdate;

        // pendingTeleport = true;
        TryApplySpawnData(spawnData.Value);
        
        Debug.Log($"We now own Shell {transform.name}", gameObject);
    }
    
    private void TryApplySpawnData(ShellSpawnData data)
    {
        if (!IsOwner)
            return;
    
        networkTransform.Teleport(data.Position, data.Rotation, transform.localScale);
    }

    
    private void Update()
    {
        if (VehicleController.IsNetworked)
        {
            OnOwnerNetworkUpdate?.Invoke();
        }
        else
        {
            OnUpdate();     // The Local-only/Testing Update
        }
    }
    
    /// <summary>
    /// The update method called when connected to a network
    /// </summary>
    public override void OwnerNetworkUpdate()
    {
        if (isPooled.Value) return;
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
    public override void OnUpdate()
    {
        // Decrement timer to 0, then deactivate and return to pool
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
            OnFixedUpdate();
        }
    }

    /// <summary>
    /// Called when the Network is active
    /// </summary>
    public override void NetworkedFixedUpdate()
    {
        if (isPooled.Value)
        {
            // Debug.Log($"{name}: still pooled, skipping move. IsOwner={IsOwner}");
            return; // If pooled (only spawnable)
        }

        // Move only if we own this object - Network Transform synchronises to every client!
        if (IsOwner)
        {
            // Debug.Log($"Moving with direction: {transform.forward * (velocity * Time.fixedDeltaTime)}");
            rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
        }
    }

    /// <summary>
    /// Called when in solo play
    /// </summary>
    public override void OnFixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
    }
    
    /// <summary>
    /// Called when this gameobject is spawned. Sets initial position and rotation.
    /// </summary>
    // [ServerRpc]
    public void Respawn()
    {
        lifetimeTimer = lifetime;
        trailRenderer.emitting = true;
        this.transform.position = VehicleController.Instance.WeaponController.shellSpawnPoint.transform.position;
        
        // Zero out X axis - the shell should always fly straight ahead
        Vector3 rotation = VehicleController.Instance.WeaponController.shellSpawnPoint.transform.rotation.eulerAngles;
        rotation.x = 0F;
        this.transform.rotation = Quaternion.Euler(rotation);
        this.transform.root.gameObject.SetActive(true);
    }

    /// <summary>
    /// Pauses functionality when released from pool
    /// </summary>
    public void Despawn()
    {
	    trailRenderer.emitting = false;
	    trailRenderer.Clear();
	    this.transform.root.gameObject.SetActive(false);
        NetworkObject.Despawn();
    }
}