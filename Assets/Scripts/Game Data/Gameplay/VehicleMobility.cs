using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Mobility", menuName = "Vehicles/Mobility Data")]
public class VehicleMobility : ScriptableObject
{
    [Header("Movement")]
    public float turnSpeed = 180.0f;
    [field: SerializeField] public float forwardSpeed { get; private set; }
    [field: SerializeField] public float forceMultiplier { get; private set; } = 150F;
    [field: SerializeField] public float backwardSpeed { get; private set; }
    [field: SerializeField] public float speedDelta { get; private set; }
    [field: SerializeField] public float brakeDelta { get; private set; }
    [field: SerializeField] public float steerVelocity { get; private set; }
    [field: SerializeField] public float physicsBounciness{ get; private set; }

    [Header("Rotation")]
    public float torqueMultipler = 30F;
    public float maxAngularVelocity = 1.35F;
    [Tooltip("How sharp the steering feels")]
    public float straightSteerDrag = 130;
    public float steeringDrag = 0F;
    
    [FormerlySerializedAs("angleLimit")]
    [Header("Stabilisation/Correction Variables")]
    public float minAngleForCorrection = 5F;
    [Tooltip("The force used to correct the vehicle rotation")]
    [FormerlySerializedAs("rotationAngleForce")] public float correctionTorqueForce = 10F;
    
    [Header("Turret")]
    public float turretRotationSpeed = 240.0f;
    
    [Header("Other")]
    public float trackMultiplier = 0.75f;
    [Tooltip("The gravity when falling or stuck")]
    [field: SerializeField] public float localGravity { get; private set; } = -32F;
    [Tooltip("The gravity when grounded")]
    [field: SerializeField] public float globalGravity { get; private set; } = -9.81F;


    [Header("Lean Settings")]
    public float minForwardVelocity = 0.3F;
    public float cruiseForwardVelocity;
    public float horizontalLeanSpeed = 8.0f;
    public float horizontalMaxLean = 15.0f;
    public float verticalLeanSpeed = 6.0f;
    public float verticalRestingLean = 4F;
    public float verticalMaxLean = 15.0f;
    [Tooltip("The value to be met which triggers the resting lean value. Ideally, should be slightly below the max lean, but not excessively")]
    public float verticalToRestingLeanValue;
}
