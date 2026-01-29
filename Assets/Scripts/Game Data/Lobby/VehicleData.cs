using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleData : Singleton<VehicleData>
{
	[SerializeField] private List<VehicleLobbyData> lobby;
	[SerializeField] private List<VehicleGameplayData> gameplay;


	private new void Awake()
	{
		base.Awake();
		
		// If this instance was destroyed by the base class, don't continue
		if (Instance != this)
			return;
		
		DontDestroyOnLoad(this);
		// nullOnDestroy = false;
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