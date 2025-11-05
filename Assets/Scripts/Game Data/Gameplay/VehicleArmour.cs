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
}