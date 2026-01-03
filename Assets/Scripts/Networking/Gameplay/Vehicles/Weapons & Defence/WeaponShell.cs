using Unity.Netcode;
using UnityEngine;

public class WeaponShell : NetworkBehaviour
{
    internal VehicleWeaponController owner;
    [SerializeField] private Rigidbody rigidBody;
    [field: SerializeField] public NetworkObject networkObject { get; private set; }
    public bool isPooled = true;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime;
    private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Movement")]
    [SerializeField] private float velocity;
    private Vector3 shellDirection;
    private float shellSpeed;


	public void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation)
    {
        this.owner = weaponController;
        transform.SetPositionAndRotation(position, rotation);
        shellDirection = rotation * Vector3.forward;
        shellSpeed = velocity;
        lifetimeTimer = lifetime;
    }

    private void Update()
    {
        if (VehicleController.IsNetworked)
        {
            OnNetworkedUpdate();
        }
        else
        {
            OnUpdate();
        }
    }

    // The update method called when connected to a network
    private void OnNetworkedUpdate()
    {
        if (isPooled) return;
        if (!IsOwner) return;

        // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            if (owner is SingleShotWeapon sWeapon)
            {
                Debug.Log("Client: Asking server to pool our expired shell");
                sWeapon.ReturnToPoolServerRpc(networkObject);
            }
            // Implement for clip weapon
        }
    }

    private void OnUpdate()
    {
        // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            if (owner is SingleShotWeapon sWeapon)
            {
                Destroy(this.gameObject);
            }
            // Implement for clip weapon
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
            OnNetworkedFixedUpdate();
        }
        else
        {
            OnFixedUpdate();
        }
    }

    /// <summary>
    /// Called when the Network is active
    /// </summary>
    private void OnNetworkedFixedUpdate()
    {
        if (isPooled) return;

        if (IsOwner)
        {
            rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
        }
        else if (rigidBody.isKinematic)
        {
            rigidBody.position += shellDirection * shellSpeed * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Called when in solo play
    /// </summary>
    private void OnFixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newDirection"></param>
    [ServerRpc(RequireOwnership = false)]
    public void RotateWithReflectionServerRPC(Vector3 newDirection)
    {
        transform.forward = newDirection;
        shellDirection = newDirection;
        ReflectClientRpc(newDirection);
    }

    [ClientRpc]
    private void ReflectClientRpc(Vector3 direction)
    {
        if (IsServer) return; // Server already handled it
    
        // Update kinematic rigidbody on clients
        transform.forward = direction;
        shellDirection = direction;
        
        Debug.Log($"Client :: Shell reflection received - Direction: {direction}");
    }


    /// <summary>
    /// Called when this gameobject is spawned. Sets initial position and rotation.
    /// </summary>
    // [ServerRpc]
    public void Respawn()
    {
        lifetimeTimer = lifetime;
        trailRenderer.emitting = true;
        this.transform.position = owner.shellSpawnPoint.transform.position;
        
        // Zero out X axis - the shell should always fly straight ahead
        Vector3 rotation = owner.shellSpawnPoint.transform.rotation.eulerAngles;
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
        networkObject.Despawn();
    }
}