using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LookAt : MonoBehaviour
{
    [SerializeField] Transform target;

    private void Update()
    {
        this.transform.LookAt(target);
    }
}
