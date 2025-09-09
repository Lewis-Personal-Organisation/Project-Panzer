using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Vehicles/Data Holder")]
public class VehicleGameplayData : ScriptableObject
{
    [Header("Misc")] 
    [SerializeField] public string name;
    [SerializeField] public VehicleType type;

    [field: SerializeField] public VehicleWeapon weapon { get; private set; }
    [field: SerializeField] public VehicleMobility mobility { get; private set; }
    [field: SerializeField] public VehicleArmour armour { get; private set; }
}
