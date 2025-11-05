using Unity.Netcode;
using UnityEngine;

public class NetworkBehaviourExtended : NetworkBehaviour
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
