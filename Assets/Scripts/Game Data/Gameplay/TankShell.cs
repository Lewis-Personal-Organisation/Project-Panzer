using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankShell : MonoBehaviour
{
    private TankWeaponController controller;
    [SerializeField] private float shellVelocity;
    [SerializeField] private float duration;
    [SerializeField] private float durationTimer;
    
    
    public TankShell Setup(TankWeaponController controller)
    {
        this.controller = controller;
        return this;
    }

    private void Update()
    {
        durationTimer -= Time.deltaTime;

        if (durationTimer <= 0)
        {
            controller.shellPool.Release(this);
            Debug.Log("Released", this.gameObject);
        }
    }

    private void FixedUpdate()
    {
        this.transform.position += this.transform.forward * (shellVelocity * Time.deltaTime);
    }

    public void Respawn()
    {
        durationTimer = duration;
        this.transform.position = controller.shellSpawnPoint.transform.position;
        this.transform.rotation = controller.shellSpawnPoint.transform.rotation;
        this.gameObject.SetActive(true);
    }

    public void Despawn()
    {
        this.gameObject.SetActive(false);
    }
}
