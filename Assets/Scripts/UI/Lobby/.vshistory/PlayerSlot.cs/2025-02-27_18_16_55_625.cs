using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LobbyManager;
using Interface.Elements.Scripts;
using System.Collections;

public class PlayerSlot : MonoBehaviour
{
	private const string SELECT_A_VEHICLE = "Select a \nVehicle";
	private const string SELECTING_VEHICLE = "Selecting \nVehicle...";

	[SerializeField] private RectTransform parentRectTransform;
	[SerializeField] private TextMeshProUGUI playerNameTitle;
	[SerializeField] private GameObject readyGameObject;
	public VehicleSlot vehicleUISlot;
	[SerializeField] private Button openVehicleSelectionButton;
	[SerializeField] private TextMeshProUGUI openVehicleSelectionButtonText;

	public bool IsFree() => !parentRectTransform.gameObject.activeInHierarchy;

	public Coroutine scaleRoutine;
	public bool isMouseHovering = false;
	public float t = 0;
	public Vector2 baseSize;
	public Vector2 endSize;


	private void Awake()
	{
		baseSize = ((RectTransform)transform).sizeDelta;
	}

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
		Debug.Log($"PlayerSlot :: Showing with {player.Data[PlayerDictionaryData.vehicleIndexKey].Value}");
		bool isOwner = LobbyManager.playerId == player.Id;
		int vehicleIndex = int.Parse(player.Data[PlayerDictionaryData.vehicleIndexKey].Value);

		// Set vehicle UI
		playerNameTitle.text = player.Data[PlayerDictionaryData.nameKey].Value;
		vehicleUISlot.LoadVehicleData(vehicleIndex);
		openVehicleSelectionButtonText.text = vehicleIndex != -1 ? string.Empty : isOwner ? SELECT_A_VEHICLE : SELECTING_VEHICLE;

		// Set UI listeners
		openVehicleSelectionButton.onClick.RemoveAllListeners();
		openVehicleSelectionButton.enabled = isOwner;
		if (isOwner)
			openVehicleSelectionButton.onClick.AddListener(delegate
			{
				UIManager.LobbyUI.chooseVehicleViewGameObject.SetActive(true);
			});

		readyGameObject.SetActive(bool.Parse(player.Data[PlayerDictionaryData.isReadyKey].Value));

		parentRectTransform.gameObject.SetActive(true);
	}

	public void Scale(bool upscale)
	{
		StopCoroutine(scaleRoutine);
		scaleRoutine =  StartCoroutine(IScale(isMouseHovering));
	}

	private IEnumerator IScale(bool grow)
	{
		//float t = isMouseHovering ? 0 : 1;
		//int target = isMouseHovering ? 1 : 0;

		while (t < 1)
		{
			t += Time.deltaTime;
			parentRectTransform.sizeDelta = Vector2.Lerp(grow ? baseSize : endSize, grow ? endSize : baseSize, t);
			yield return null;
		}

		parentRectTransform.sizeDelta = Vector2.Lerp(grow ? baseSize : endSize, grow ? endSize : baseSize, 1);
	}

	public void SetReady(bool isReady) => readyGameObject.SetActive(isReady);
}