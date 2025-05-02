using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LobbyManager;
using Interface.Elements.Scripts;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayerSlot : MonoBehaviour
{
	private const string SELECT_A_VEHICLE = "Select a \nVehicle";
	private const string SELECTING_VEHICLE = "Selecting \nVehicle...";

	[SerializeField] private RectTransform parentRectTransform;
	[SerializeField] private RectTransform scalingTransform;
	[SerializeField] private TextMeshProUGUI playerNameTitle;
	[SerializeField] private GameObject readyGameObject;
	public VehicleSlot vehicleUISlot;
	[SerializeField] private Button openVehicleSelectionButton;
	[SerializeField] private TextMeshProUGUI openVehicleSelectionButtonText;
	[SerializeField] private EventTrigger rescaleEventTrigger;
	public bool IsFree() => !parentRectTransform.gameObject.activeInHierarchy;
	public void SetReady(bool isReady) => readyGameObject.SetActive(isReady);

	private Coroutine scaleRoutine;
	float scaleTimer = 0;
	

	/// <summary>
	/// Called when all of the Player slots are synchronised
	/// </summary>
	public void Show(string playerName, int vehicleIndex)
	{
		playerNameTitle.text = playerName;
		vehicleUISlot.LoadVehicleData(vehicleIndex);

		if (vehicleIndex != -1)
		{
			openVehicleSelectionButtonText.enabled = false;
		}

		parentRectTransform.gameObject.SetActive(true);
	}

	/// <summary>
	/// Hide a slot when it is no longer used. For example, when the player count drops
	/// </summary>
	public void Hide()
	{
		playerNameTitle.text = string.Empty;
		vehicleUISlot.LoadVehicleData(-1);
		SetReady(false);
		parentRectTransform.gameObject.SetActive(false);
	}

	public void ConfigureAndShow(Unity.Services.Lobbies.Models.Player player)
	{
		// Debug.Log($"PlayerSlot :: Showing with {player.Data[PlayerDictionaryData.vehicleIndexKey].Value}");
		bool isOwner = LobbyManager.playerId == player.Id;
		int vehicleIndex = int.Parse(player.Data[PlayerDictionaryData.vehicleIndexKey].Value);

		// Set vehicle UI
		playerNameTitle.text = player.Data[PlayerDictionaryData.nameKey].Value;
		vehicleUISlot.LoadVehicleData(vehicleIndex);
		openVehicleSelectionButtonText.text = vehicleIndex != -1 ? string.Empty : isOwner ? SELECT_A_VEHICLE : SELECTING_VEHICLE;

		// Set UI listeners
		openVehicleSelectionButton.onClick.RemoveAllListeners();
		openVehicleSelectionButton.enabled = isOwner;
		rescaleEventTrigger.triggers = null;
		
		if (isOwner)
		{
			openVehicleSelectionButton.onClick.AddListener(delegate
			{
				UIManager.LobbyUI.chooseVehicleViewGameObject.SetActive(true);
			});
			
			EventTrigger.Entry scaleUp = new EventTrigger.Entry();
			scaleUp.eventID = EventTriggerType.PointerEnter;
			scaleUp.callback.AddListener((eventData) => Scale(true));
			
			EventTrigger.Entry scaleDown = new EventTrigger.Entry();
			scaleDown.eventID = EventTriggerType.PointerExit;
			scaleDown.callback.AddListener((eventData) => Scale(false));
			
			rescaleEventTrigger.triggers.Add(scaleUp);
			rescaleEventTrigger.triggers.Add(scaleDown);
		}

		readyGameObject.SetActive(bool.Parse(player.Data[PlayerDictionaryData.isReadyKey].Value));
		parentRectTransform.gameObject.SetActive(true);
	}
	
	public void Scale(bool upscale)
	{
		if (scaleRoutine != null)
			StopCoroutine(scaleRoutine);

		scaleRoutine =  StartCoroutine(IEScale(upscale));
	}

	private IEnumerator IEScale(bool grow)
	{
		float speed = UIManager.LobbyUI.playerSlotResizeTime;
		
		while (true)
		{
			scaleTimer = Mathf.Clamp(scaleTimer + (grow ? Time.deltaTime / speed : -(Time.deltaTime / speed)), 0F, 1F);
			parentRectTransform.localScale = Vector2.one + Vector2.one * UIManager.LobbyUI.sizeCurve.Evaluate(scaleTimer);
			yield return null;
		}
	}
}