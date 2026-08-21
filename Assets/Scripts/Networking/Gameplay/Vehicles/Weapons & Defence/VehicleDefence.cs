using System;
using System.Collections.Generic;
using UnityEngine;

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
    [Serializable]
    public class HitDebug
    {
        public Vector3 point;
        public Vector3 fromPos;
        public Vector3 toPos;
        public bool didRotate;
        public Extensions.ReflectResult reflectResult;
        public float timer = 3F;
    }
    
    
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
            TakeDamageLocal(Extensions.TankSide.Front, 55);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].timer > 0)
            {
                if (hits[i].point != Vector3.zero)
                    Gizmos.DrawWireSphere(hits[i].point, .1F);

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
            triggerEvent.Other.transform.root.TryGetComponent(out WeaponAmmoBehaviour ammunition);
            // SceneData.Label("Hits Received: ", $"{++hitsTaken}");
            
            HitDebug hit = new HitDebug();
            hit.point = triggerEvent.Other.transform.position;
            hit.fromPos = triggerEvent.Other.transform.position + -triggerEvent.Other.transform.forward * debugRayDistance;
            hit.toPos = triggerEvent.Other.transform.position;
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdv(triggerEvent.Other.transform, minAngleForRicochet);
            hit.reflectResult = reflectResult;
            
            SceneData.Label("Last bullet Ricochet?: ", $"{reflectResult.didRicochet} - {reflectResult.direction}");

            if (reflectResult.didRicochet)
            {
                // Check if not near 0
                if (reflectResult.direction.sqrMagnitude > 0.001f)
                {
                    ammunition.RotateWithReflectionLocal(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                    Debug.Log($"Server :: Shell reflected - Direction: {reflectResult.direction.normalized}");
                    hit.didRotate = true;
                }
            }
            else
            {
                string builtName = ammunition.ownerName.Value.Value;
                Debug.Log($"VehicleDefence :: We took a hit from {builtName}");
                TakeDamage(reflectResult.tankSide, ammunition.baseDamage);
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
            if (ammunition.NetworkObject.IsOwner)
                return;
            
            SceneData.Label("Hits Received: ", $"{++hitsTaken}");
            
            HitDebug hit = new HitDebug();
            hit.point = triggerEvent.Other.transform.position;
            hit.fromPos = triggerEvent.Other.transform.position + -triggerEvent.Other.transform.forward * debugRayDistance;
            hit.toPos = triggerEvent.Other.transform.position;
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = ((BoxCollider)triggerEvent.Caller).ReflectWithAngleAdv(triggerEvent.Other.transform, minAngleForRicochet);
            hit.reflectResult = reflectResult;
            
            SceneData.Label("Last bullet Ricochet?: ", $"{reflectResult.didRicochet} - {reflectResult.direction}");

            if (reflectResult.didRicochet)
            {
                // Check if not near 0
                if (reflectResult.direction.sqrMagnitude > 0.001f)
                {
                    ammunition.RotateWithReflectionServerRPC(reflectResult.direction.normalized);
                    vehicle.cameraController.Shake(vehicleArmour.OnRicochetEnemyShakeParams);
                    Debug.Log($"Server :: Shell reflected - Direction: {reflectResult.direction.normalized}");
                    hit.didRotate = true;
                }
            }
            else
            {
                string builtName = ammunition.ownerName.Value.Value;
                Debug.Log($"VehicleDefence :: We took a hit from {builtName}");
                TakeDamage(reflectResult.tankSide, ammunition.baseDamage);
                vehicle.cameraController.Shake(vehicleArmour.OnHitEnemyShakeParams);
                hit.didRotate = false;
            }
            
            hits.Add(hit);
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
            GameplayUI.Notifications.QueueNetworkNotif($"Player {GameplayNetworkManager.Instance.localPlayerName} was destroyed!");
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
