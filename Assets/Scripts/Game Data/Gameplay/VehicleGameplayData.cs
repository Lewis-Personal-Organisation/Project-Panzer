using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class VehicleGameplayData : ScriptableObject
{
    [Header("Misc")] 
    [SerializeField] public string name;
    [SerializeField] public VehicleType type;

    [field: SerializeField] public TankWeapon tankWeapon { get; private set; }
    
    [Header("Turret")]
    [SerializeField] private float turretRotationSpeed;         // Acts as Gun Rotation Speed, if no turret is present
    
    [Header("Armour")]
    [SerializeField] private float frontThickness;
    [SerializeField] private float sideThickness;
    [SerializeField] private float rearThickness;
    
    [Header("Mobility")]
    [SerializeField] private float acceleration;
    [field: SerializeField] public float forwardSpeed { get; private set; }
    [field: SerializeField] public float backwardSpeed { get; private set; }
    [field: SerializeField] public float neutralSteeringInfluence{ get; private set; }        // The forced speed for steering
}
