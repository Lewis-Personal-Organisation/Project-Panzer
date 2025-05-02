using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class VehicleGameplayData : ScriptableObject
{
    [Header("Misc")] 
    [SerializeField] private string name;
    [SerializeField] public VehicleType type;

    [Header("Weapon")]
    [SerializeField] private float reloadTime;
    [SerializeField] private int shellsInClip;
    [SerializeField] private float shellVelocity;
    
    [Header("Turret")]
    [SerializeField] private float turretRotationSpeed;         // Acts as Gun Rotation Speed, if no turret is present
    
    [Header("Armour")]
    [SerializeField] private float frontThickness;
    [SerializeField] private float sideThickness;
    [SerializeField] private float rearThickness;
    
    [Header("Mobility")]
    [SerializeField] private float acceleration;
    [SerializeField] private float forwardsTopSpeed;
    [SerializeField] private float backwardsTopSpeed;
    [SerializeField] private bool neutralSteering;              // Implement later
}
