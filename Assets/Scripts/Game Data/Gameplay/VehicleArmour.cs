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

    public float GetThickness(Extensions.TankSide side)
    {
        switch (side)
        {
            case Extensions.TankSide.Front: return frontThickness;
            case Extensions.TankSide.Left:
            case Extensions.TankSide.Right: return sideThickness;
            case Extensions.TankSide.Back: return rearThickness;
        }

        return 0F;
    }
}