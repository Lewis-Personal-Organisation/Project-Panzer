using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

[Serializable]
public class HitDebug
{
    public Vector3 point;
    public Vector3 fromPos;
    public Vector3 toPos;
    public bool didRotate;
    public Extensions.ReflectResult reflectResult;
    public float timer = 200F;
}

public class VehicleDefence : VehicleComponent, IVehicleComponentToggleable
{
    [SerializeField] private VehicleArmour vehicleArmour;
    [SerializeField] private TriggerDelegator triggerDelegator;
    [SerializeField] private float health = 100;
    [SerializeField] private LayerMask shellMask;
    public float minAngleForRicochet = 0;
    private int hitsTaken = 0;

    public float debugRayDistance = 3;
    public float debugSphereSize;

    public List<HitDebug> hits = new List<HitDebug>();
    
    
    
    public void Enable()
    {
        triggerDelegator.enabled = true;
        health = 100;
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
            TakeDamageLocal(Extensions.BoxColliderHitSide.Front, 55);
        }
    }
    
    private void OnDrawGizmos()
    {
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].timer > 0)
            {
                if (hits[i].point != Vector3.zero)
                {
                    Gizmos.color = hits[i].didRotate ? Color.magenta : Color.red;
                    Gizmos.DrawWireSphere(hits[i].point, .2F);
                }

                if (hits[i].didRotate)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(hits[i].fromPos, hits[i].toPos);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(hits[i].toPos, hits[i].reflectResult.direction * debugRayDistance);
                }

                hits[i].timer -= EditorApplicationUpdater.DeltaTime;
            }
        }

        for (int i = hits.Count - 1; i > -1; i--)
        {
            if (hits[i].timer <= 0)
                hits.RemoveAt(i);
        }
    }

    public void CustomOnTriggerEnterLocalOnly(OnTriggerDelegation triggerEvent)
    {
        if ((shellMask.value & 1 << triggerEvent.Other.gameObject.layer) != 0)
        {
            Transform shellTransform = triggerEvent.Other.transform;
            Vector3 shellPosition = shellTransform.position;
            shellTransform.root.TryGetComponent(out WeaponAmmoBehaviour ammo);
            
            HitDebug hit = new HitDebug();
            hit.point = shellPosition;
            hit.fromPos = shellPosition + -shellTransform.forward * debugRayDistance;
            hit.toPos = shellPosition;
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdvFromDirection(shellPosition, shellTransform.forward, minAngleForRicochet);
            hit.reflectResult = reflectResult;
            
            SceneData.Label("Last bullet Ricochet?: ", $"{reflectResult.didRicochet} - {reflectResult.direction}");

            if (reflectResult.didRicochet)
            {
                // Check if not near 0
                if (reflectResult.direction.sqrMagnitude > 0.001f)
                {
                    ammo.RotateWithReflectionLocal(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                    Debug.Log($"Server :: Shell reflected - Direction: {reflectResult.direction.normalized}");
                    hit.didRotate = true;
                }
            }
            else
            {
                Debug.Log($"VehicleDefence :: We took a hit from {ammo.spawnData.Value.OwnerName}");
                
                if (TakeDamage(reflectResult.boxColliderHitSide, ammo.baseDamage))
                {
                    vehicle.Destroy();
                }
                
                vehicle.cameraController.Shake(vehicleArmour.OnHitEnemyShakeParams);
                hit.didRotate = false;
            }
            
            hits.Add(hit);
        }
    }

    /// <summary>
    /// Called by the Trigger Delegator to handle the shell trigger event
    /// </summary>
    /// <param name="triggerEvent"></param>
    public void CustomOnTriggerEnter(OnTriggerDelegation triggerEvent)
    {
        // TODO: SELF-HIT filter

        // If not running network, stop
        if (!VehicleController.IsNetworked)
            return;

        if ((shellMask.value & 1 << triggerEvent.Other.gameObject.layer) == 0)
            return;

        triggerEvent.Other.transform.root.TryGetComponent(out WeaponAmmoBehaviour ammo);

        Vector3 shellForward = ammo.transform.forward;
        Vector3 shellPosition = ammo.transform.position;

        // Reflect the target transform if its hits our Box Collider at or above ricochet angle
        Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdvFromDirection(shellPosition, shellForward, minAngleForRicochet);

        Debug.Log($"Hit: {(reflectResult.didRicochet ? "Deflected" : "Absorbed")}\n" +
                  $"Angle: {reflectResult.hitAngle}\n" +
                  $"Side: {reflectResult.boxColliderHitSide}\n");

        // Ricochet
        if (reflectResult.didRicochet && reflectResult.direction.sqrMagnitude > 0.001f)
        {
            // If we own this ammo, Rotate on our client, to avoid RTT via RPC
            if (ammo.IsOwner)
            {
                ammo.RotateWithOwner(reflectResult.direction.normalized);
            }
            else
            {
                // If this is another players ammo, shake our camera
                if (vehicle)
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
            }
        }
        // Non-Ricochet
        else
        {
            // Disable visuals
            ammo.ToggleVisuals(false);
            ammo.IncreaseUseAmountServerRPC();
            
            // Owned Client Player tank - take damage, shake cam, send notif
            // Only send Notif on the actual destroyed local client tank
            if (vehicle)
            {
                vehicle.cameraController?.Shake(vehicleArmour.OnHitEnemyShakeParams);
                
                if (TakeDamage(reflectResult.boxColliderHitSide, ammo.baseDamage))
                    GameplayUI.Notifications.Request($"{ammo.spawnData.Value.OwnerName.Value} destroyed {GameplayNetworkManager.localPlayerAvatar.name}");
            }
        }
    }

    /// <summary>
    /// Causes this vehicle to take damage. Returns whether the damage destroyed this vehicle
    /// </summary>
    private bool TakeDamage(Extensions.BoxColliderHitSide side, float baseDamage)
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
        
        Debug.Log($"Hit taken! => Side {side} | Damage: {damage} | New Health: {health}");

        if (health <= 0F)
        {
            vehicle.Destroy();
            return true;
        }
        
        return false;
    }

    private void TakeDamageLocal(Extensions.BoxColliderHitSide side, float baseDamage)
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
