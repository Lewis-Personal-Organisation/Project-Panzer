using System.Globalization;
using TMPro;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.UI;
using static LobbyManager;

//[System.Serializable]
public class PlayerSlot : MonoBehaviour
{
	private const string SELECT_A_VEHICLE = "Select A \nVehicle";
	private const string SELECTING_VEHICLE = "Select A \nVehicle";

	[SerializeField] private GameObject parent;
	[SerializeField] private TextMeshProUGUI playerNameTitle;
	[SerializeField] private GameObject readyGameObject;
	public VehicleSlot vehicleUISlot;
	[SerializeField] private Button openVehicleSelectionButton;
	[SerializeField] private TextMeshProUGUI openVehicleSelectionButtonText;

	public bool IsFree() => !parent.activeInHierarchy;

	/// <summary>
	/// Called when all of the Player slots are synchronised
	/// </summary>
	public void Show(string playerName, int vehicleIndex)
	{
		playerNameTitle.text = playerName;
		vehicleUISlot.LoadVehicleData(vehicleIndex);

		if (vehicleIndex != -1)
		{
			var myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
			openVehicleSelectionButtonText.enabled = false;
			vehicleUISlot.SetStatsGroupVisible(true);
		}

		parent.SetActive(true);
	}

	/// <summary>
	/// Hide a slot when it is no longer used. For example, when the player count drops
	/// </summary>
	public void Hide()
	{
		playerNameTitle.text = string.Empty;
		parent.SetActive(false);
	}

	public void ConfigureAndShow(Unity.Services.Lobbies.Models.Player player)
	{
		bool isOwner = LobbyManager.playerId == player.Id;

		playerNameTitle.text = player.Data[PlayerDictionaryData.nameKey].Value;
		vehicleUISlot.LoadVehicleData(int.Parse(player.Data[PlayerDictionaryData.vehicleIndexKey].Value));

		openVehicleSelectionButton.onClick.RemoveAllListeners();
		openVehicleSelectionButton.gameObject.SetActive(isOwner);
		if (isOwner)
			openVehicleSelectionButton.onClick.AddListener(delegate
			{
				UIManager.LobbyUI.chooseVehicleViewGameObject.SetActive(true);
			});


		// Owner
		if (LobbyManager.playerId == playerID)
		{

		}
		// Not Owner
		else
		{

		}
	}

	/// <summary>
	/// Enable/Disabled buttons events for if we own these buttons
	/// </summary>
	//public void ConfigureOpenVehicleSelectionButton(bool isOwner)
	//{
	//	openVehicleSelectionButton.onClick.RemoveAllListeners();

	//	if (isOwner)
	//	{
	//		//Debug.Log($"Added listener to Open Veh Sel Button");
	//		openVehicleSelectionButton.onClick.AddListener(delegate
	//		{
	//			UIManager.LobbyUI.chooseVehicleViewGameObject.SetActive(true);
	//			//Debug.Log($"Clicked on our Vehicle Selection Button");
	//		});
	//	}

	//	openVehicleSelectionButton.gameObject.SetActive(isOwner);
	//}

	public void SetReady(bool isReady) => readyGameObject.SetActive(isReady);
}