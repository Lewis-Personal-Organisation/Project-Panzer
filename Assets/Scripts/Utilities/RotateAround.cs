using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public Transform target;
    public float yOffset;
    public float radius = 100F;
    public float angle = 0f;
    public float rotationSpeed = 1F;

    
    void Update()
    {
        angle += rotationSpeed * Time.deltaTime;
        this.transform.position = new Vector3(target.position.x + Mathf.Cos(angle) * radius, target.position.y + yOffset, target.position.z + Mathf.Sin(angle) * radius);
    }
}
