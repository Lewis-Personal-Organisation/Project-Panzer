using UnityEngine;

[CreateAssetMenu]
public class TankWeapon : ScriptableObject
{
    [field: SerializeField] public TankShell shellPrefab { get; private set; }
    [SerializeField] private int ammoCount;
    [field: SerializeField] public float reloadTime { get; private set; }
}
