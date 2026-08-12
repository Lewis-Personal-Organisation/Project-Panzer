using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffector : MonoBehaviour
{
    public Rigidbody rb;
    public bool hit;
    
    // external velocity
    public Vector3 externalVelocity;
    public float externalVelocityTimer;
    public float externalVelocityDuration;


    private void FixedUpdate()
    {
        rb.velocity = GetCurrentExternalVelocity();
    }


    public void AddExternalVelocity(Vector3 velocity, float duration)
    {
        externalVelocity = velocity;
        externalVelocityDuration = duration;
        externalVelocityTimer = duration;
    }

    private Vector3 GetCurrentExternalVelocity()
    {
        if (externalVelocityTimer <= 0f)
            return Vector3.zero;

        externalVelocityTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(externalVelocityTimer / externalVelocityDuration);
        return externalVelocity * t;
    }
}
