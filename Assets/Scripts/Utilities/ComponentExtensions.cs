using UnityEngine;

public static class ComponentExtensions
{
    public static void TryGetLocalComponent<T>(this MonoBehaviour mono, ref T component) where T : Component
    {
        if (component != null)
            return;

        mono.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{mono.gameObject.name} :: {mono.name} :: {component.name} was not found!");
    }
    
    private static T AssignLocal<T>(MonoBehaviour mono, T component) where T : Component
    {
        if (component != null)
            return component;
    
        mono.gameObject.TryGetComponent(out component);
        
        if (!component)
            Debug.LogError($"{mono.gameObject.name} :: {mono.name} :: {component.name} was not found!");

        return component;
    }
}
