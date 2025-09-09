using UnityEngine;

[CreateAssetMenu(fileName = "Vehicle Weapon", menuName = "Vehicles/Weapon Data")]
public class VehicleWeapon : ScriptableObject
{
    [field: SerializeField] public TankShell shellPrefab { get; private set; }
    [field: SerializeField] public int ammoCount { get; private set; }
    [field: SerializeField] public float reloadTime { get; private set; }
    
    // Shot Lean Data
    [field: SerializeField] public float xLeanMax { get; private set; }
    [field: SerializeField] public float zLeanMax { get; private set; }
    [field: SerializeField] public float leanTime { get; private set; }
}
