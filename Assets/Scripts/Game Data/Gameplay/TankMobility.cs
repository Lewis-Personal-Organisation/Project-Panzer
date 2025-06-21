using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tank Mobility", menuName = "Tanks/Mobility Data")]
public class TankMobility : ScriptableObject
{
    [SerializeField] private float acceleration;
    [field: SerializeField] public float forwardSpeed { get; private set; }
    [field: SerializeField] public float backwardSpeed { get; private set; }
    [field: SerializeField] public float neutralSteeringInfluence{ get; private set; }  
    public float TurnSpeed = 180.0f;
    public float turretSpeed = 240.0f;
    public float trackMultiplier = 0.75f;
    [Header("Lean Settings")]
    public float HleanSpeed = 8.0f;
    public float VleanSpeed = 6.0f;
    public float HMaxLean = 15.0f;
    public float VMaxLean = 15.0f;
}
