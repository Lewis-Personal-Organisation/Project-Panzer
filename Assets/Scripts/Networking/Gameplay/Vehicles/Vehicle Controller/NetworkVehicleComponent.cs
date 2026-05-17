using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkVehicleComponent : NetworkBehaviour
{
    /// <summary>
    /// Attempts to assign a local component if not already assigned. Error if the component is required
    /// </summary>
    protected void TryGetComponent<T>(ref T component, bool required = true) where T : Component
    {
        if (component != null) 
            return;
    
        this.gameObject.TryGetComponent(out component);
        
        if (!component && required)
            Debug.LogError($"{this.gameObject.name} :: {typeof(T).Name} was not found!");
    }
    
    protected T TryGetComponentAdv<T>(ref T component, bool required = true) where T : Component
    {
        if (component != null) 
            return component;
    
        this.gameObject.TryGetComponent(out component);
        
        if (component == null && required)
            Debug.LogError($"{this.gameObject.name} :: {typeof(T).Name} was not found!");

        return component;
    }
}
