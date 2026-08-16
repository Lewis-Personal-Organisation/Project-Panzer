using System;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/*  This class is attached to a moving project or 'Shell'
 *  It is responsible for moving the projectile on every client
 *  Whether Authoritive or not
 */

public class WeaponShell : WeaponAmmoBehaviour, IDebuggable
{
    // public VehicleWeaponController owner;
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

    /// <summary>
    /// Called by the server or Locally in non-networked scenarios
    /// </summary>
	public override void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation)
    {
        // owner = weaponController;

        // ownerName is assigned by the caller (which knows the actual firing client's ID),
        // since this shell may be initialized here before it is owned by the shooter.

        transform.SetPositionAndRotation(position, rotation);
        shellDirection = rotation * Vector3.forward;
        shellSpeed = velocity;
        lifetimeTimer = lifetime;
    }
    
    /// <summary>
    /// Called when the server gives ownership to the owning player. Called on both Server and Owner
    /// </summary>
    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        
        // Excludes the server, if the server is not the owner
        if (!IsOwner)
            return;
        
        // owner = VehicleController.Instance.WeaponController;
        
        lifetimeTimer = lifetime;
        shellSpeed = velocity;
        
        OnOwnerNetworkUpdate = OwnerNetworkUpdate;
        OnNetworkFixedUpdate = NetworkedFixedUpdate;
        
        Debug.Log($"We now own Shell {transform.name}", gameObject);
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
        if (isPooled) return;
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
        if (isPooled) return;   // If pooled (only spawnable)

        // Move only if we own this object - Network Transform synchronises to every client!
        if (IsOwner)
        {
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