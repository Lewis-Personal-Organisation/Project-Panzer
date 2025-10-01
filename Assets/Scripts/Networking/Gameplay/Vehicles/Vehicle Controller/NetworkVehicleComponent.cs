using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkVehicleComponent : NetworkBehaviour
{
    protected void TryGetComponent<T>(ref T component) where T : Component
    {
        if (component != null) 
            return;
    
        this.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{this.gameObject.name} :: {component.name} was not found!");
    }
    
    protected void TryGetComponentOnGameObject<T>(GameObject parent, ref T component) where T : Component
    {
        if (component != null) 
            return;
    
        parent.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{parent.gameObject.name} :: {component.name} was not found!");
    }
    
    protected void TryAssignComponentValue<TComponent, TValue>(ref TValue field, Func<TComponent, TValue> selector) where TComponent : Component
    {
        if (field != null)
            return;

        if (gameObject.TryGetComponent(out TComponent comp))
        {
            field = selector(comp);
        }
        else
        {
            Debug.LogError($"{gameObject.name} :: {typeof(TComponent).Name} was not found!", this.gameObject);
        }
    }
}
