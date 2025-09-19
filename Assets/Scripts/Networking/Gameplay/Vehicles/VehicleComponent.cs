using UnityEngine;

public class VehicleComponent : MonoBehaviour
{
    protected VehicleController vehicle;
    
    
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
