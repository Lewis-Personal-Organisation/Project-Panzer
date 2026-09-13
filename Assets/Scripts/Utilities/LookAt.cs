using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix;
using Sirenix.OdinInspector;

public class LookAt : MonoBehaviour
{
    [SerializeField] Transform target;

    [DisableIf("@target == null")]
    [Button(ButtonSizes.Medium), GUIColor(0.929411765F, 0.270588235f, 0.270588235F)]
    private void LookAtTarget()
    {
        this.transform.LookAt(target);
    }
    
    private void Update()
    {
        this.transform.LookAt(target);
    }
}
