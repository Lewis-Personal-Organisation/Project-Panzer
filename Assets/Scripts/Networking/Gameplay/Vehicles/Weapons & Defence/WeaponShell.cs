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

        trailTime = trailRenderer.time;
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
    
    private void TryApplySpawnData(ShellSpawnData newData)
    {
        if (IsOwner)
        {
            Debug.Log($"We now own Shell {transform.name}. It was {(newData.Pooled ? "pooled" : "fired")}", gameObject);
            networkTransform.Teleport(newData.Position, newData.Rotation, transform.localScale);

            // If Spawned, setup movement etc
            if (!newData.Pooled)
            {
                lifetimeTimer = lifetime;
                shellSpeed = velocity;
            
                OnOwnerNetworkUpdate = OwnerNetworkUpdate;
                OnNetworkFixedUpdate = NetworkedFixedUpdate;
            
                ToggleVisuals(true);
            }
        }

        // If returned to pool, notify clients
        if (newData.Pooled)
        {
            Debug.Log("Clients (All): Expired shell as requested from Server");
        }
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
    public override void OnFixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
    }
}