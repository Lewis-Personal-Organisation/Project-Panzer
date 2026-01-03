using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Utilities;

public class VehicleDefence : VehicleComponent, IVehicleComponentToggleable
{
    [SerializeField] private VehicleArmour vehicleArmour;
    [SerializeField] private TriggerDelegator triggerDelegator;
    [SerializeField] private float health = 100;
    [SerializeField] private LayerMask shellMask;
    public float minAngleForRicochet = 0;
    private int hitsTaken = 0;

    public void Enable()
    {
        triggerDelegator.enabled = true;
    }

    public void Disable()
    {
        triggerDelegator.enabled = false;
    }

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
            // If the vehicle isn't setup, it's not ours
            // Call tank effects here? For example hit VFX?
            return;
        }

        // If not running network, stop
        if (!VehicleController.IsNetworked)
            return;
        
        if ((shellMask.value & 1 << triggerEvent.Other.gameObject.layer) != 0)
        {
            triggerEvent.Other.transform.root.TryGetComponent(out WeaponShell shell);
            
            // Return if this is our shell!
            if (shell.networkObject.IsOwner == true)
                return;
            
            SceneData.Label("Hits Received: ", $"{++hitsTaken}");
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdv(triggerEvent.Other.transform, minAngleForRicochet);

            SceneData.Label("Last bullet Ricochet?: ", $"{reflectResult.didRicochet} - {reflectResult.direction}");

            if (reflectResult.didRicochet)
            {
                // Check if not near 0
                if (reflectResult.direction.sqrMagnitude > 0.001f)
                {
                    shell.RotateWithReflectionServerRPC(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                    Debug.Log($"Server :: Shell reflected - Direction: {reflectResult.direction.normalized}");
                }
            }
            else
            {
                TakeDamage(reflectResult.tankSide);
                vehicle.cameraController.Shake(vehicleArmour.OnHitEnemyShakeParams);
            }
        }
    }

    /// <summary>
    /// SHOULD IMPLEMENT DAMAGE TAKEN
    /// </summary>
    private void TakeDamage(Extensions.TankSide side)
    {
        SceneData.Label("Last Hit Normal: ", $"{side}");
        vehicle.Disable();
    }
}
