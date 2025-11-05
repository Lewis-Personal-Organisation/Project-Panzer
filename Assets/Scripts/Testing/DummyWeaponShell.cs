using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class DummyWeaponShell : MonoBehaviour
{
    internal DummyWeaponController controller;
    [SerializeField] private NetworkObject networkObject;
    public bool IsOwner => networkObject.IsOwner;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime;
    private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Movement")]
    [SerializeField] private float velocity;


    public DummyWeaponShell Setup(DummyWeaponController controller)
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
    public void Respawn(Vector3 direction)
    {
        lifetimeTimer = lifetime;
        trailRenderer.emitting = true;
        this.transform.position = controller.shellSpawnPoint.transform.position;
        
        // Zero out X axis - the shell should always fly straight ahead
        direction.y = 0F;
        Quaternion rot = Quaternion.LookRotation(direction);
        this.transform.rotation = rot;
        
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
