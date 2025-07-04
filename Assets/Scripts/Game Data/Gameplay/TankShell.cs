using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TankShell : MonoBehaviour
{
    private TankWeaponController controller;
    [FormerlySerializedAs("shellVelocity")] [SerializeField] private float velocity;
    [FormerlySerializedAs("duration")] [SerializeField] private float lifetime;
    [FormerlySerializedAs("durationTimer")] [SerializeField] private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Shell Guide")]
	public Transform shellVisuals;
	public Transform shellGuiderTR;
	public Transform projectileTip;
	public float projectileRotationSpeed = 5;
	[FormerlySerializedAs("terrainHitMask")] public LayerMask rotationDetectionMask;


	public TankShell Setup(TankWeaponController controller)
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
		shellGuiderTR.transform.position += shellGuiderTR.transform.forward * (velocity * Time.deltaTime);
		shellVisuals.transform.position = shellGuiderTR.position;
		AdjustAngleToGround();
    }

    /// <summary>
    /// Called when this gameobject is spawned. Sets initial position and rotation.
    /// </summary>
    public void Respawn()
    {
        lifetimeTimer = lifetime;
        trailRenderer.emitting = true;
        shellGuiderTR.transform.position = controller.shellSpawnPoint.transform.position;
        shellGuiderTR.transform.rotation = controller.shellSpawnPoint.transform.rotation;
        AdjustAngleToGround();
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Pauses functionality when released from pool
    /// </summary>
    public void Despawn()
    {
	    trailRenderer.emitting = false;
	    trailRenderer.Clear();
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Rotates the shell visuals to match the shell guide, if raycast is succesfull
    /// </summary>
	private void AdjustAngleToGround()
	{
		if (!Physics.Raycast(projectileTip.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, rotationDetectionMask.value))
		{
			Debug.DrawLine(projectileTip.position, projectileTip.position + Vector3.down * Mathf.Infinity, Color.red, Time.deltaTime);
			return;
		}

		// Debug.DrawLine(projectileTip.position, projectileTip.position + Vector3.down * Mathf.Infinity, Color.green, Time.deltaTime);

		shellGuiderTR.rotation = Quaternion.FromToRotation(shellGuiderTR.up, hit.normal) * shellGuiderTR.rotation;
		shellVisuals.transform.rotation = Quaternion.RotateTowards(shellVisuals.transform.rotation, shellGuiderTR.rotation, projectileRotationSpeed * Time.deltaTime);
	}
}
