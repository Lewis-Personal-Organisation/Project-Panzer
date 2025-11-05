using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoBehaviourExt : MonoBehaviour
{
    internal void TryGetLocalComponent<T>(ref T component) where T : Component
    {
        if (component != null) 
            return;

        this.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{this.gameObject.name} :: {component.name} was not found!");
    }
}
