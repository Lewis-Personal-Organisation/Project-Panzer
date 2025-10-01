using UnityEngine;

public class VehicleDefence : MonoBehaviour
{
    [SerializeField] private VehicleArmour vehicleArmour;
    [SerializeField] private float health = 100;
    [SerializeField] private LayerMask shellMask;
    private int instanceID;
    
    
    private void Awake()
    {
        instanceID = this.gameObject.GetInstanceID();
    }


    private void OnTriggerEnter(Collider other)
    {
        if ((shellMask.value & 1 << other.gameObject.layer) != 0)
        {
            if (!other.transform.root.TryGetComponent(out WeaponShell shell))
                return;
            
            // If shell controller ID matches ours, this must be our shell, return;
            if (instanceID == shell.controller.gameObject.GetInstanceID())
                return;

            Debug.Log($"Shell intercepted by other gameobject! {gameObject.name}. Is owner: {shell.IsOwner}");
        }
    }
}
