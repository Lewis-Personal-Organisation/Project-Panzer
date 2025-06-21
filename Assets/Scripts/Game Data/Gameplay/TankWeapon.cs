using UnityEngine;

[CreateAssetMenu(fileName = "Tank Weapon", menuName = "Tanks/Weapon Data")]
public class TankWeapon : ScriptableObject
{
    [field: SerializeField] public TankShell shellPrefab { get; private set; }
    [SerializeField] private int ammoCount;
    [field: SerializeField] public float reloadTime { get; private set; }
}
