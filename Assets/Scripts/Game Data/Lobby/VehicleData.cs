using System.Collections.Generic;
using UnityEngine;

public class VehicleData : Singleton<VehicleData>
{
	[SerializeField] private List<VehicleLobbyData> data;


	new private void Awake()
	{
		base.Awake();
	}

	public VehicleLobbyData GetItem(int index)
	{
		if (data.Count > index)
			return data[index];

		return null;
	}

	public int ItemCount() => data.Count;
}