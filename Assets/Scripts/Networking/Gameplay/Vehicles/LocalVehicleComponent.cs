using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalVehicleComponent : VehicleComponent
{
    protected void Awake()
    {
        TryGetLocalComponent(ref vehicle);
    }
    
    public void TryGetLocalComponent<T>(ref T component) where T : Component
    {
        if (component != null) 
            return;

        this.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{this.gameObject.name} :: {component.name} was not found!");
    }
}
