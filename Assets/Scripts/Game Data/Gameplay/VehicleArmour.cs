using UnityEngine;

[CreateAssetMenu(fileName = "Armour", menuName = "Vehicles/Armour Data")]
public class VehicleArmour : ScriptableObject
{
    [SerializeField] private float frontThickness;
    [SerializeField] private float sideThickness;
    [SerializeField] private float rearThickness;
}