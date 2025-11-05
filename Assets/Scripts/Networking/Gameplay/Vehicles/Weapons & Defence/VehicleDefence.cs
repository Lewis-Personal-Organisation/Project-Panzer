using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class VehicleDefence : VehicleComponent
{
    [SerializeField] private VehicleArmour vehicleArmour;
    [SerializeField] private TriggerDelegator triggerDelegator;
    [SerializeField] private float health = 100;
    [SerializeField] private LayerMask shellMask;
    public float minAngleForRicochet = 0;
    private int hitsTaken = 0;

    
    public void Setup(VehicleController owner)
    {
        vehicle = owner;
    }

    /// <summary>
    /// Called by the Trigger Delegator to handle the shell trigger event
    /// </summary>
    /// <param name="triggerEvent"></param>
    public void CustomOnTriggerEnter(OnTriggerDelegation triggerEvent)
    {
        if (!vehicle)
        {
            // If the vehicle isn't setup, its not ours
            // Call tank effects here? For example hit VFX?
            return;
        }
        
        if ((shellMask.value & 1 << triggerEvent.Other.gameObject.layer) != 0)
        {
            triggerEvent.Other.transform.root.TryGetComponent(out WeaponShell shell);
            
            if (shell.networkObject.IsOwner == false)
            {
                // This is not our shell, must be enemy shell
                OnGUISceneViewData.AddOrUpdateLabel("Enemy shells: ", $"{++hitsTaken}");
            }
            else
            {
                return;     // Don't take a hit from our shell!
            }
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdv(triggerEvent.Other.transform, minAngleForRicochet);

            OnGUISceneViewData.AddOrUpdateLabel("Last bullet Ricochet?: ", $"{reflectResult.didRicochet} - {reflectResult.direction}");

            if (reflectResult.didRicochet)
            {
                // Check if not near 0
                if (reflectResult.direction.sqrMagnitude > 0.001f)
                {
                    shell.RotateWithReflectionServerRPC(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                }
            }
            else
            {
                TakeDamage();
                vehicle.cameraController.Shake(vehicleArmour.OnHitEnemyShakeParams);
            }
        }
    }

    /// <summary>
    /// SHOULD IMPLEMENT DAMAGE TAKEN
    /// </summary>
    private void TakeDamage()
    {
        
    }
}
