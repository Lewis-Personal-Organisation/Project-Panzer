using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class SceneryMissile : NetworkBehaviour
{
    public enum ProjectileStage
    {
        None,
        Launch,
        MidFlight,
        Impact
    }

    private NetworkVariable<ProjectileStage> networkedStage = new NetworkVariable<ProjectileStage>(
        ProjectileStage.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [FoldoutGroup("Transforms")] public Transform missilePlatform;
    [FoldoutGroup("Transforms")] public Transform target;
    
    [FoldoutGroup("Collisions")] [SerializeField] private TriggerDelegator platformTrigger;
    [FoldoutGroup("Collisions")] public BoxCollider detonateCollider;
    [FoldoutGroup("Collisions")] [SerializeField] private LayerMask collidableMask;
    
    [FoldoutGroup("Particles")] public ParticleSystem engineParticles;
    [FoldoutGroup("Particles")] public ParticleSystem groundParticles;
    [FoldoutGroup("Particles")] public ParticleSystem hitParticlesA;
    [FoldoutGroup("Particles")] public ParticleSystem hitParticlesB;
    [FoldoutGroup("Particles")] public ParticleSystem detonateParticlesA;
    [FoldoutGroup("Particles")] private float groundParticleStartSize = 0;
    
    [FoldoutGroup("Behaviour Variables")] public float velocity;
    [FoldoutGroup("Behaviour Variables")] public float ascentVelocity;
    [FoldoutGroup("Behaviour Variables")] public float descentVelocity;
    [FoldoutGroup("Behaviour Variables")] public float ascentAccel = 7F;
    [FoldoutGroup("Behaviour Variables")] public float descentAccel = 14F;
    [FoldoutGroup("Behaviour Variables")] public float ascentTime = 8F;
    [FoldoutGroup("Behaviour Variables")] public float rotationSpeed;

    [SerializeField] private float timer = 0F;
    private bool canRotate = true;
    private bool detonate = false;
    public float radiusSpeed = 1F;
    public float detonationRadius = 0F;
    public float detonationRadiusMax = 1F;
    public float minDetonationForce = 1.5F;
    public float maxDetonationForce = 5F;
    public float detonationForcePushTimer = 1.5F;

    public bool editorDebug = false;
    
    private Vector3 localDetonatePosition;

    private BehaviourSequence behaviourSequence;
    
    public List<ExplosionEffector> debugTargets = new List<ExplosionEffector>();
    
    [DisableIf("@!EditorApplication.isPlaying")]
    [Button(ButtonSizes.Medium), GUIColor(0.929411765F, 0.270588235f, 0.270588235F)]
    private void Launch()
    {
        SetNetworkState(ProjectileStage.Impact);
        // behaviourSequence = MissileBehaviour();
    }

    /// <summary>
    /// When this missile is spawned on a new client, subscribe and sync to its current state
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            platformTrigger.Disable();
        }
        
        // Register for network changes
        networkedStage.OnValueChanged += ApplyStage;
        ApplyStage(ProjectileStage.None, networkedStage.Value);
        
        // Queued
        GameplayNetworkManager.OnLocalPlayerAssigned += () => Debug.Log($"Registered for network changes of SceneryMissile for {GameplayNetworkManager.Instance.localPlayerName}");
    }
    
    public override void OnNetworkDespawn()
    {
        networkedStage.OnValueChanged -= ApplyStage;
    }

    // Called from the server to change state
    private void SetNetworkState(ProjectileStage newStage)
    {
        #if UNITY_EDITOR
        if (NetworkManager.Singleton)
            networkedStage.Value = newStage;
        #else
            networkedStage.Value = newStage;
        #endif
    }

    /// <summary>
    /// The method to run on all clients when network variables changes
    /// </summary>
    private void ApplyStage(ProjectileStage oldStage, ProjectileStage newStage)
    {
        switch (newStage)
        {
            case ProjectileStage.Launch:
                hitParticlesA.Play(false);
                hitParticlesB.Play(false);
                groundParticles.Play(false);
                engineParticles.Play(false);
                break;

            case ProjectileStage.MidFlight:
                groundParticles.Stop();
                break;

            case ProjectileStage.Impact:
                engineParticles.transform.SetParent(null);
                engineParticles.Stop(false);

                // Re-apply the cached local position, Saving sending over network on detonation
                if (!IsServer)
                    detonateParticlesA.transform.localPosition = localDetonatePosition;
                
                detonateParticlesA.transform.SetParent(null);
                detonateParticlesA.transform.rotation = Quaternion.Euler(Vector3.zero);
                detonateParticlesA.Play(false);
                
                // Start Coroutine on the Player, as we deactivate this gameobject 
                VehicleController.Instance.StartCoroutine(DoExplosion());
                
                enabled = false;
                this.gameObject.SetActive(false);
                break;
        }
    }
    
    private IEnumerator DoExplosion()
    {
        while (true)
        {
            detonationRadius += Time.deltaTime * radiusSpeed;

            for (int i = 0; i < debugTargets.Count; i++)
            {
                if (debugTargets[i].hit)
                    continue;
                
                float dist = Vector3.Distance(detonateParticlesA.transform.position, debugTargets[i].transform.position);

                if (dist < detonationRadius)
                {
                    Vector3 force = (debugTargets[i].transform.position - detonateParticlesA.transform.position).normalized;
                    force.y = 0;
                    force *= Mathf.Lerp(maxDetonationForce, minDetonationForce, dist / detonationRadiusMax);
                    debugTargets[i].AddExternalVelocity(force, detonationForcePushTimer);
                    Debug.Log($"Explosion: Cube {i} {dist} <= radius {detonationRadius}, radius max {detonationRadiusMax}, force {force}");
                    debugTargets[i].hit = true;
                }
            }
            
            float playerDist = Vector3.Distance(detonateParticlesA.transform.position, VehicleController.Instance.centerTransform.position);
            Debug.Log($"Scaling Explosion: Radius: {detonationRadius}, Player Dist:{playerDist}");
            
            if (playerDist <= detonationRadius)
            {
                Vector3 force = (VehicleController.Instance.centerTransform.position - detonateParticlesA.transform.position).normalized;
                force.y = 0;
                force *= Mathf.Lerp(maxDetonationForce, minDetonationForce, playerDist / detonationRadiusMax);
                VehicleController.Instance.bodyMover.AddExternalVelocity(force, detonationForcePushTimer);
                Debug.Log($"Explosion: Player dist {playerDist} <= radius {detonationRadius}, radius max {detonationRadiusMax}, force {force}");
                yield break;
            }
            
            // Explosion expired
            if (detonationRadius > detonationRadiusMax)
            {
                Debug.Log("Explosion expired");
                yield break;
            }
            
            yield return null;
        }
    }
    
    [ExecuteAlways]
    private void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying)
        {
            if (detonationRadius is > 0 or < 0)
            {
                detonationRadius = 0;
            }
        }

        if (!editorDebug)
            return;
        
        Gizmos.DrawWireSphere(detonateParticlesA.transform.position, detonationRadiusMax);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detonateParticlesA.transform.position, detonationRadius);
        detonationRadius += EditorApplicationUpdater.DeltaTime * radiusSpeed;
            
        if (detonationRadius > detonationRadiusMax)
            detonationRadius = 0F;
    }

    private void Start()
    {
        // Detach the ground particles
        groundParticles.transform.SetParent(null);
        missilePlatform.SetParent(null);
        
        groundParticleStartSize = groundParticles.main.startSize.constantMax;

        if (IsServer)
        {
            behaviourSequence = MissileBehaviour();
            behaviourSequence.isPaused = true;
        }
        else
        {
            localDetonatePosition = detonateParticlesA.transform.localPosition;
        }
    }

    public BehaviourSequence MissileBehaviour()
    {
        target = GameplaySceneManager.Instance.UnusedRocketTarget();

        BehaviourStep LaunchAndRise = new BehaviourStep(
            () => SetNetworkState(ProjectileStage.Launch),
            () =>
            {
                if (velocity < ascentVelocity)
                    velocity += Time.fixedDeltaTime * ascentAccel;

                transform.position += transform.up * (velocity * Time.fixedDeltaTime);
                timer += Time.deltaTime;

                ParticleSystem.MainModule gpMain = groundParticles.main;
                gpMain.startSize = Mathf.Lerp(groundParticleStartSize, 0, Mathf.InverseLerp(1, 2.5F, timer)); // Shrink start size from 1s, ending at 5s
            },
            () => timer > ascentTime,
            () =>
            {
                timer = 0F;
                SetNetworkState(ProjectileStage.MidFlight);
            },
            false);

        BehaviourStep travelToTarget = new BehaviourStep(null,
            () =>
            {
                if (canRotate)
                {
                    Quaternion dirToTarget = Quaternion.LookRotation(target.transform.position - transform.position) * Quaternion.Euler(90, 0, 0);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, dirToTarget, rotationSpeed * Time.fixedDeltaTime);

                    if (Quaternion.Angle(transform.rotation, dirToTarget) < 0.1f)
                    {
                        if (velocity < descentVelocity)
                            velocity += Time.fixedDeltaTime * descentAccel;
                    }
                }

                transform.position += transform.up * (velocity * Time.fixedDeltaTime);
            },
            () => detonate,
            () => SetNetworkState(ProjectileStage.Impact),
            false);

        return new BehaviourSequence(false, LaunchAndRise, travelToTarget);
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            behaviourSequence?.Process();
        }

        // Local Non-networked
        #if UNITY_EDITOR
        if (!NetworkManager.Singleton)
        {
            behaviourSequence?.Process();
        }
        #endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SetNetworkState(ProjectileStage.Impact);
    }

    /// <summary>
    /// The callback for routed OnTriggerEnter Calls
    /// </summary>
    public void OnCustomTriggerEnter(OnTriggerDelegation triggerEvent)
    {
        ResolveCollision(triggerEvent.Other.transform);
    }

    /// <summary>
    /// The callback for routed OnCollisionEnter Calls
    /// </summary>
    public void OnCustomCollisionEnter(OnCollisionDelegation collisionEvent)
    {
        ResolveCollision(collisionEvent.Collision.transform);
    }

    private void ResolveCollision(Transform other)
    {
        Debug.Log($"{this.transform.name} was hit by {other.root.gameObject.name}", this.gameObject);

        // If we were hit by anything other than a player or shell, return 
        if ((collidableMask.value & 1 << other.root.gameObject.layer) == 0)
            return;

        // If missile network state is already launched, don't retrigger
        if (networkedStage.Value != ProjectileStage.None)
            return;

        behaviourSequence.isPaused = false;
    }

    /// <summary>
    /// Detonates the missile when we touch an object
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Stop future collisions
        detonateCollider.enabled = false;

        // Stop missile rotation
        canRotate = false;

        // Set for detonation
        detonate = true;
    }
}