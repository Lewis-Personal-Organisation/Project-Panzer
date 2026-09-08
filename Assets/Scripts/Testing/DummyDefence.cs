using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class DummyDefence : MonoBehaviour
{
    [SerializeField]
    private BoxCollider collider;
    [SerializeField] private LayerMask shellMask;
    public float minAngleForRicochet = 0;
    [SerializeField] private int hitsTaken = 0;
    
    
    public float debugRayDistance = 3;
    
    public List<HitDebug> hits = new List<HitDebug>();
    public Color debugHit, debugBounce, debugResult;
    public float sphereSize, lineThickness;
    
    
    private void OnTriggerEnter(Collider other)
    {
         if ((shellMask.value & 1 << other.gameObject.layer) != 0)
        {
            other.transform.root.TryGetComponent(out WeaponAmmoBehaviour ammo);
            hitsTaken++;
            
            HitDebug hit = new HitDebug();
            hit.point = other.transform.position;
            hit.fromPos = other.transform.position + -other.transform.forward * debugRayDistance;
            hit.toPos = other.transform.position;
            
            
            // Reflect the target transform if its hits our Box Collider at or above ricochet angle
            Extensions.ReflectResult reflectResult = collider.ReflectWithAngleAdvFromDirection(other.transform.position, ammo.transform.forward, minAngleForRicochet);
            hit.reflectResult = reflectResult;

            if (reflectResult.didRicochet && reflectResult.direction.sqrMagnitude > 0.001f)
            {
                // Check if not near 0
                if (!NetworkManager.Singleton)
                {
                    ammo.transform.forward = reflectResult.direction.normalized;
                }
                else
                {
                    ammo.RotateWithOwner(reflectResult.direction.normalized);
                }
                Debug.Log($"Shell reflected - Direction: {reflectResult.direction.normalized}");
                hit.didRotate = true;
            }
            else
            {
                Debug.Log($"Shell Absorbed - Direction: {reflectResult.direction.normalized}");
                Destroy(ammo.gameObject);
                hit.didRotate = false;
            }
            
            hits.Add(hit);
        }
    }
    
    private void OnDrawGizmos()
    {

        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].timer > 0)
            {
                Handles.color = hits[i].didRotate ? debugBounce : debugHit;
                
                if (hits[i].point != Vector3.zero)
                    Handles.SphereHandleCap(
                        0,                   // Control ID (0 makes it non-clickable)
                        hits[i].point,       // Position
                        Quaternion.identity, // Rotation
                        sphereSize,          // Size
                        EventType.Repaint    // Event type
                    );
                    // Gizmos.DrawWireSphere(hits[i].point, .25F);

                if (hits[i].didRotate)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawDottedLine(hits[i].fromPos, hits[i].toPos, lineThickness);
                    Handles.color = debugResult;
                    Handles.DrawLine(hits[i].toPos, hits[i].toPos + hits[i].reflectResult.direction * debugRayDistance, lineThickness);
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
}
