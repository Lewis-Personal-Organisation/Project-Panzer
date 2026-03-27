using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropulsionTrailController : MonoBehaviour
{
    [SerializeField] private TrailRenderer leftTrail;
    [SerializeField] private Rigidbody leftRigidbody;
    [SerializeField] private Transform target;
    // private int _lastPositionCount;

    private void Start()
    {
        leftTrail.transform.SetParent(null);
    }


    private void Update()
    {
        leftTrail.transform.position = target.position;
        // int currentCount = leftTrail.positionCount;
        // if (currentCount > _lastPositionCount)
        // {
        //     leftTrail.transform.position = transform.position;
        // }
        // _lastPositionCount = currentCount;
    }
}
