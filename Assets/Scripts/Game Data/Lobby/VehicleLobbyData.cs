using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum VehicleType
{
	SPG,
}

[CreateAssetMenu]
public class VehicleLobbyData : ScriptableObject
{
	public Sprite icon;
	public VehicleType type;
	[Range(0, 1)] public float firepower;
	[Range(0, 1)] public float mobility;
	[Range(0, 1)] public float defence;
}

// GAMEPLAY USE
//public struct Armour
//{
//	[SerializeField] private float frontThickness;
//	[SerializeField] private float sideThickness;
//	[SerializeField] private float backThickness;
//}