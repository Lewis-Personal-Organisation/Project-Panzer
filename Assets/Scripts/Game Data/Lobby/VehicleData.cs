using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleData : Singleton<VehicleData>
{
	// public static int ItemCount() => Instance.data.Count;
	[SerializeField] private List<VehicleLobbyData> lobby;
	[SerializeField] private List<VehicleGameplayData> gameplay;


	new private void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(this);
	}

	public static VehicleLobbyData GetLobbyItem(int index)
	{
		if (Instance.lobby.Count > index)
			return Instance.lobby[index];

		return null;
	}

	public static VehicleGameplayData GetGameplayItem(int index)
	{
		if (Instance.gameplay.Count > index)
			return Instance.gameplay[index];

		return null;
	}
}