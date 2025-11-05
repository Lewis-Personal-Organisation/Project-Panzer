using MilkShake;
using UnityEngine;

[CreateAssetMenu(fileName = "Dummy Weapon", menuName = "Dummy/Weapon Data")]
public class DummyWeapon : ScriptableObject
{
    [field: SerializeField] public DummyWeaponShell shellPrefab { get; private set; }
    [field: SerializeField] public int ammoCount { get; private set; }
    [field: SerializeField] public float reloadTime { get; private set; }
    
    [Tooltip("The shake we process when we take a hit from an enemy")]
    [field: SerializeField] public ShakeParameters OnHitEnemyShakeParams { get; private set; }
}