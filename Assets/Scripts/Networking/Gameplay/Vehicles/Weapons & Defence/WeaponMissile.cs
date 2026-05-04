using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class WeaponMissile : WeaponAmmoBehaviour
{
    internal VehicleWeaponController owner;
    [SerializeField] private Rigidbody rigidBody;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime;
    private float lifetimeTimer;
    [SerializeField] private TrailRenderer trailRenderer;
	
    [Header("Movement")]
    [SerializeField] private float velocity;
    [SerializeField] private float velocityMax;
    [SerializeField] private float velocityDelta;
    private float shellSpeed;


    private class WeaponBehaviourStep
    {
        private Action behaviour;
        private Func<bool> completeCondition;
        private Action onComplete;
        private bool isComplete = false;
        private bool stopOnComplete = false;

        public WeaponBehaviourStep(Action behaviour, Func<bool> completeCondition, Action onComplete, bool stopOnComplete)
        {
            this.behaviour = behaviour;
            this.completeCondition = completeCondition;
            this.onComplete = onComplete;
            this.onComplete += () => isComplete = true;
            this.stopOnComplete = stopOnComplete;
        }

        public bool Process()
        {
            if (stopOnComplete && isComplete)
                return true;
            
            behaviour.Invoke();

            if (completeCondition == null || completeCondition != null && completeCondition())
            {
                onComplete?.Invoke();
                return true;
            }

            return false;
        }
    }

    // private WeaponBehaviourStep[] behaviourSteps;
    
    [SerializeField] private float movementTimer;
    [SerializeField] private int behaviourStep = 0;
    [SerializeField] private float stepZeroGravity;
    public Vector3 targetRotation = new Vector3(-90F, 0, 0);
    public float rotationSpeed = 5f;
    [SerializeField] private float angle = 0;
    
    BehaviourSequence missileBehaviourSequence;
    
    
    public override void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation)
    {
        this.owner = weaponController;
        transform.SetPositionAndRotation(position, rotation);
        shellDirection = rotation * Vector3.forward;
        shellSpeed = velocity;
        lifetimeTimer = lifetime;
        velocityMax = velocity * 2F;

        BehaviourStep launchStep = new BehaviourStep(() =>
            {
                trailRenderer.emitting = false;
                rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime) + transform.up * stepZeroGravity * Time.fixedDeltaTime);
                movementTimer += Time.deltaTime;
            },
            () => movementTimer > .35F,
            null,
            false);

        BehaviourStep altitudeIncreaseStep = new BehaviourStep(() =>
            {
                trailRenderer.emitting = true;
                Quaternion currentRotation = transform.rotation;
                Quaternion desiredRotation = Quaternion.Euler(targetRotation);
                rigidBody.rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * rotationSpeed);
                velocity = Mathf.MoveTowards(velocity, velocityMax, Time.deltaTime * velocityDelta);
                rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
            },
            () => Quaternion.Angle(transform.rotation, Quaternion.Euler(targetRotation)) < 0.1f,
            null,
            false);

        BehaviourStep forwardVelocity = new BehaviourStep(
            () =>
            {
                velocity = Mathf.MoveTowards(velocity, velocityMax, Time.deltaTime * velocityDelta);
                rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
            },
            () => rigidBody.position.y > 75F,
            () =>
            {
                angle = UnityEngine.Random.Range(0f, 360f); // Pick random angle
            },
            false);

        BehaviourStep rotateAndLand = new BehaviourStep(() =>
            {
                // Rotate towards new angle and move forward
                rigidBody.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(90F, angle, 0)), Time.deltaTime * rotationSpeed * 0.2F);
                rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
            },
            () => rigidBody.position.y <= 0,
            null,
            false);
        
        
        missileBehaviourSequence = new BehaviourSequence();
        missileBehaviourSequence.AddStep(launchStep);
        missileBehaviourSequence.AddStep(altitudeIncreaseStep);
        missileBehaviourSequence.AddStep(forwardVelocity);
        missileBehaviourSequence.AddStep(rotateAndLand);
    }

    private void Update()
    {
        if (VehicleController.IsNetworked)
        {
            OnNetworkedUpdate();
        }
        else
        {
            OnUpdate();
        }
    }
    
    private void FixedUpdate()
    {
        if (VehicleController.IsNetworked)
        {
            OnNetworkedFixedUpdate();
        }
        else
        {
            OnFixedUpdate();
        }
    }

    public override void OnNetworkedUpdate()
    {
        if (isPooled) return;
        if (!IsOwner) return;

        // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            Debug.Log("Client: Asking server to pool our expired shell");
            owner.ReturnToPoolServerRpc(networkObject);
        }
    }
    public override void OnUpdate()
    {
        // Decrement timer to 0, then deactivate and return to pool
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0)
        {
            Destroy(this.gameObject);
        }
    }
    public override void OnNetworkedFixedUpdate()
    {
        if (isPooled) return;

        if (IsOwner)
        {
            rigidBody.MovePosition(rigidBody.position + transform.forward * (velocity * Time.fixedDeltaTime));
        }
        else if (rigidBody.isKinematic)
        {
            rigidBody.position += shellDirection * shellSpeed * Time.fixedDeltaTime;
        }
    }
    public override void OnFixedUpdate()
    {
        missileBehaviourSequence.Process();
    }
}
