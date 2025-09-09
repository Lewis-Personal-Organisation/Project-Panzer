using UnityEngine;

[CreateAssetMenu(fileName = "Tank Weapon", menuName = "Tanks/Weapon Data")]
public class TankWeapon : ScriptableObject
{
    [field: SerializeField] public TankShell shellPrefab { get; private set; }
    [field: SerializeField] public int ammoCount { get; private set; }
    [field: SerializeField] public float reloadTime { get; private set; }
    
    // Shot Lean Data
    [field: SerializeField] public float xLeanMax { get; private set; }
    [field: SerializeField] public float zLeanMax { get; private set; }
    [field: SerializeField] public float leanTime { get; private set; }
}
