using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class ProjectileTest : MonoBehaviour
{
    public Transform shellVisuals;
    [FormerlySerializedAs("debugVisuals")] public Transform shellGuiderTR;
    [FormerlySerializedAs("ProjectileTip")] public Transform projectileTip;
    public float moveSpeed;
    public float projectileRotationSpeed = 5;
    public LayerMask terrainHitMask;

    
    // Update is called once per frame
    void Update()
    {
        Move();
        AdjustAngleToGround();
    }

    private void Move()
    {
        shellGuiderTR.position += shellGuiderTR.forward * (Time.deltaTime * moveSpeed);
    }

    private void AdjustAngleToGround()
    {
        if (!Physics.Raycast(projectileTip.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainHitMask.value))
        {
            Debug.DrawLine(projectileTip.position, projectileTip.position + Vector3.down * Mathf.Infinity, Color.red, Time.deltaTime);
            return;
        }

        Debug.DrawLine(projectileTip.position, projectileTip.position + Vector3.down * Mathf.Infinity, Color.green, Time.deltaTime);

        shellGuiderTR.rotation = Quaternion.FromToRotation(shellGuiderTR.up, hit.normal) * shellGuiderTR.rotation;
        shellVisuals.transform.position = shellGuiderTR.position;
        shellVisuals.transform.rotation = Quaternion.RotateTowards(shellVisuals.transform.rotation, shellGuiderTR.rotation, projectileRotationSpeed * Time.deltaTime);
    }
}