using Unity.Netcode;
using UnityEngine;

public class WeaponShell : MonoBehaviour
{
    internal VehicleWeaponController controller;
    [SerializeField] private NetworkObject networkObject;
    public bool IsOwner => networkObject.IsOwner;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime;
    private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Movement")]
    [SerializeField] private float velocity;


	public WeaponShell Setup(VehicleWeaponController controller)
    {
        this.controller = controller;
        return this;
    }

    private void Update()
    {
	    // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            controller.shellPool.Release(this);
        }
    }

    /// <summary>
    /// Moves the shell guide and visuals.
    /// Visual shell is Rotated towards new rotation 
    /// </summary>
    private void FixedUpdate()
    {
		this.transform.position += this.transform.forward * (velocity * Time.deltaTime);
		// AdjustAngleToGround();
    }

    /// <summary>
    /// Called when this gameobject is spawned. Sets initial position and rotation.
    /// </summary>
    public void Respawn()
    {
        lifetimeTimer = lifetime;
        trailRenderer.emitting = true;
        this.transform.position = controller.shellSpawnPoint.transform.position;
        
        // Zero out X axis - the shell should always fly straight ahead
        Vector3 rotation = controller.shellSpawnPoint.transform.rotation.eulerAngles;
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
    }
}