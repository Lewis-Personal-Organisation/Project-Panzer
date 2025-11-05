using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkVehicleComponent : NetworkBehaviour
{
    protected void TryGetComponent<T>(ref T component, bool required = true) where T : Component
    {
        if (component != null) 
            return;
    
        this.gameObject.TryGetComponent(out component);
        
        if (!component && required)
            Debug.LogError($"{this.gameObject.name} :: {component.name} was not found!");
    }
}
