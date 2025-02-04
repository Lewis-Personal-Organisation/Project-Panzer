using System.Collections.Generic;
using UnityEngine;

public class VehicleData : Singleton<VehicleData>
{
	[SerializeField] private List<VehicleLobbyData> data;


	new private void Awake()
	{
		base.Awake();
	}

	public static VehicleLobbyData GetItem(int index)
	{
		if (Instance.data.Count > index)
			return Instance.data[index];

		return null;
	}

	public static int ItemCount() => Instance.data.Count;
}