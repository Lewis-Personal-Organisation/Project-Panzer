using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tank Data", menuName = "Tanks/Data Holder")]
public class VehicleGameplayData : ScriptableObject
{
    [Header("Misc")] 
    [SerializeField] public string name;
    [SerializeField] public VehicleType type;

    [field: SerializeField] public TankWeapon weapon { get; private set; }
    [field: SerializeField] public TankMobility mobility { get; private set; }
    [field: SerializeField] public TankArmour armour { get; private set; }
}
