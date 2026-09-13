using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Orbit : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    
    
    public float yOffset;
    private float radiusZoomT;
    private float initialRadius;
    private float currentRadius;
    public float radius = 100F;
    public float radiusZoomTime = 2;
    public float angle = 0f;
    public float rotationSpeed = 1F;
    
    [DisableIf("@target == null")]
    [Button(ButtonSizes.Medium), GUIColor(0.929411765F, 0.270588235f, 0.270588235F)]
    private void LookAtTarget()
    {
        this.transform.LookAt(target);
    }

    private void Awake()
    {
        if (target == null) 
            return;

        // 1. Get the direction vector from the target to this object
        Vector3 direction = transform.position - target.position;

        // 2. Calculate the initial angle on the XZ plane using Atan2
        // Atan2 takes (y, x), which maps to (z, x) in Unity's 3D space
        angle = Mathf.Atan2(direction.z, direction.x);

        initialRadius = Mathf.Min(5, radius);
    }

    void Update()
    {
        if (target == null) 
            return;

        radiusZoomT += Time.deltaTime / radiusZoomTime;
        currentRadius = Mathf.Lerp(initialRadius, radius, radiusZoomT);
        
        angle += rotationSpeed * Time.deltaTime;
        this.transform.position = new Vector3(target.position.x + Mathf.Cos(angle) * currentRadius, target.position.y + yOffset, target.position.z + Mathf.Sin(angle) * currentRadius);
        this.transform.LookAt(target);
    }
}
