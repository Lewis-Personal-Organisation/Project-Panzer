using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoBehaviourExt : MonoBehaviour
{
    // Enables the "Enable/Disable" tick box in the inspector
    #if UNITY_EDITOR
    private void Start() { }
    #endif

    internal void TryGetLocalComponent<T>(ref T component) where T : Component
    {
        if (component != null) 
            return;

        this.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{this.gameObject.name} :: {component.name} was not found!");
    }
}
