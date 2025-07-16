using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Tank Mobility", menuName = "Tanks/Mobility Data")]
public class TankMobility : ScriptableObject
{
    [SerializeField] private float acceleration;
    [field: SerializeField] public float speedChangeDelta { get; private set; }
    [field: SerializeField] public float forwardSpeed { get; private set; }
    [field: SerializeField] public float backwardSpeed { get; private set; }
    [field: SerializeField] public float neutralSteeringInfluence{ get; private set; }  
    public float TurnSpeed = 180.0f;
    public float turretSpeed = 240.0f;
    public float trackMultiplier = 0.75f;
    [FormerlySerializedAs("HleanSpeed")] [Header("Lean Settings")]
    public float horizontalLeanSpeed = 8.0f;
    [FormerlySerializedAs("VleanSpeed")] public float verticalLeanSpeed = 6.0f;
    [FormerlySerializedAs("HMaxLean")] public float horizontalMaxLean = 15.0f;
    [FormerlySerializedAs("VMaxLean")] public float verticalMaxLean = 15.0f;
}
