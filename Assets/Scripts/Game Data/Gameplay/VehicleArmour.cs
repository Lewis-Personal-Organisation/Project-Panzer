using System;
using System.Diagnostics;
using MilkShake;
using UnityEngine;


[CreateAssetMenu(fileName = "Armour", menuName = "Vehicles/Armour Data")]
public class VehicleArmour : ScriptableObject
{
    [SerializeField] private float frontThickness;
    [SerializeField] private float sideThickness;
    [SerializeField] private float rearThickness;
    
    [field: SerializeField] public ShakeParameters OnHitEnemyShakeParams { get; private set; }
    [field: SerializeField] public ShakeParameters OnRicochetEnemyShakeParams { get; private set; }

    public float GetThickness(Extensions.BoxColliderHitSide side)
    {
        switch (side)
        {
            case Extensions.BoxColliderHitSide.Front: return frontThickness;
            case Extensions.BoxColliderHitSide.Left:
            case Extensions.BoxColliderHitSide.Right: return sideThickness;
            case Extensions.BoxColliderHitSide.Back: return rearThickness;
        }

        return 0F;
    }
}