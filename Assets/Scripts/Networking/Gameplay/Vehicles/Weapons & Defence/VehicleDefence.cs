using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TakeDamageLocal(Extensions.TankSide.Front, 55);
        }
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
            triggerEvent.Other.transform.root.TryGetComponent(out WeaponAmmoBehaviour ammunition);
            
            // Return if this is our shell!
            if (ammunition.networkObject.IsOwner == true)
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
                    ammunition.RotateWithReflectionServerRPC(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                    Debug.Log($"Server :: Shell reflected - Direction: {reflectResult.direction.normalized}");
                }
            }
            else
            {
                string builtName = ammunition.ownerName.Value.Value;
                Debug.Log($"VehicleDefence :: We took a hit from {builtName}");
                TakeDamage(reflectResult.tankSide, ammunition.baseDamage);
                vehicle.cameraController.Shake(vehicleArmour.OnHitEnemyShakeParams);
            }
        }
    }

    /// <summary>
    /// SHOULD IMPLEMENT DAMAGE TAKEN
    /// </summary>
    private void TakeDamage(Extensions.TankSide side, float baseDamage)
    {
        // Get the thickness for the side of vehicle that was hit
        float thickness = vehicleArmour.GetThickness(side);
        float damage = baseDamage - thickness * 0.075F;         // base 25 dmg subtract (80 * 0.075) => 6 = 19
        health = Mathf.Clamp(health - damage, 0F, 100);
        
        // Activate FX
        // switch (side)
        // {
        //     case Extensions.TankSide.Front:
        //         break;
        //     case Extensions.TankSide.Right:
        //         break;
        //     case Extensions.TankSide.Back:
        //         break;
        //     case Extensions.TankSide.Left:
        //         break;
        // }
        
        Debug.Log($"Hit taken! => Side hit: {side} | Damage: {damage} | New Health: {health}");

        if (health <= 0F)
        {
            vehicle.Destroy();
            GameplayUI.Notifications.GlobalMessage($"Player {GameplayNetworkManager.Instance.localPlayerName} was destroyed!");
        }
    }

    private void TakeDamageLocal(Extensions.TankSide side, float baseDamage)
    {
        // Get the thickness for the side of vehicle that was hit
        float thickness = vehicleArmour.GetThickness(side);
        float damage = baseDamage - thickness * 0.075F;         // base 25 dmg subtract (80 * 0.075) => 6 = 19
        health = Mathf.Clamp(health - damage, 0F, 100);
        
        Debug.Log($"Hit taken! => Side hit: {side} | Damage: {damage} | New Health: {health}");

        if (health <= 0F)
        {
            vehicle.Destroy();
        }
    }
}
