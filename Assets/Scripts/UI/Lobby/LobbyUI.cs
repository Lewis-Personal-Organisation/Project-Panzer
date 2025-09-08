using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static LobbyManager;

public class LobbyUI : Panel
{
	[SerializeField] private TextMeshProUGUI title;
	[SerializeField] private Button leaveButton;
	[SerializeField] private Button readyButton;
	[SerializeField] private TextMeshProUGUI readyButtonText;
	[SerializeField] private Color readyColour;
	[SerializeField] private Color unreadyColour;
	private bool isReady;
	public Coroutine readyStateCoroutine;
	public Coroutine vehicleButtonsStateCoroutine;

	[Header("Join Code UI")]
	[SerializeField] private GameObject joinCodeGroup;
	[SerializeField] private Button joinCodeCopyButton;
	[SerializeField] private TextMeshProUGUI joinCodeText;

	public GameObject chooseVehicleViewGameObject;

	private List<Player> activeLobbyPlayers => LobbyManager.Instance.activeLobby.Players;

	[Header("Player Slots")]
	public float playerSlotResizeTime;
	public Vector2 playerSlotEndSize;
	public AnimationCurve sizeCurve;
	[SerializeField] private PlayerSlot[] playerSlots = new PlayerSlot[4];
	
	[Header("Vehicle Slots")]
	[SerializeField] private VehicleSlot[] vehicleSlots = new VehicleSlot[5];

	

	private void Awake()
	{
		// Setup Vehicle Select slots
		for (int i = 0; i < vehicleSlots.Length; i++)
		{
			vehicleSlots[i].LoadVehicleData(vehicleSlots[i].dataIndex);
		}

		leaveButton.onClick.AddListener(OnLeaveClicked);
		readyButton.onClick.AddListener(OnReadyClicked);
		joinCodeCopyButton.onClick.AddListener(CopyJoinCode);
	}

	private void Start()
	{
		LobbyManager.OnLobbyChanged += OnLobbyChanged;
	}

	/// <summary>
	/// Alert Unity Lobby we have quit the active Lobby
	/// </summary>
	private void OnApplicationQuit()
	{
		LobbyManager.Instance?.LeaveLobbyOnQuit();
	}

	public void OnLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		AdjustPlayerSlots();
		AdjustLocalReadyButton();

		if (LobbyManager.Instance.isHost)
		{
			OnHostLobbyChanged(updatedLobby, isGameReady);
		}
		else
		{
			OnClientLobbyChanged(updatedLobby, isGameReady);
		}
	}

	private void AdjustLocalReadyButton()
	{
		for (int i = 0; i < LobbyManager.Instance.activeLobby.Players.Count; i++)
		{
			if (LobbyManager.playerId == LobbyManager.Instance.activeLobby.Players[i].Id)
			{
				isReady = bool.Parse(LobbyManager.Instance.activeLobby.Players[i].Data[PlayerDictionaryData.isReadyKey].Value);
				readyButton.image.color = isReady ? readyColour : unreadyColour;
				readyButtonText.text = isReady ? "Unready" : "Ready";
				
			}
		}
	}

	private async Task OnHostLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		if (isGameReady)
		{
			await LobbyManager.Instance.activeLobbyEvents.UnsubscribeAsync();
			LobbyManager.Instance.activeLobbyEvents = null;
			
			UIManager.Instance.UpdateGameView(View.Gameplay);
			
			NetworkManager.Singleton.SceneManager.LoadScene(SceneHelper.Instance.mainGameplayScene.Name, LoadSceneMode.Single);
			Debug.Log($"LobbyUI :: OnHostLobbyChanged :: Everyone is ready! Cancelling activeLobbyEvents and Loading Gameplay Scene");
		}
	}

	private async Task OnClientLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		if (isGameReady)
		{
			await LobbyManager.Instance.activeLobbyEvents.UnsubscribeAsync();
			LobbyManager.Instance.activeLobbyEvents = null;			
			
			UIManager.Instance.UpdateGameView(View.Gameplay);

			Debug.Log($"LobbyUI :: OnJoinLobbyChanged :: Everyone is ready! Cancelling activeLobbyEvents and Loading Gameplay Scene");
		}
	}

	public Panel Prepare(string lobbyCode, string lobbyTittle)
	{
		//readyButton.interactable = true;
		leaveButton.interactable = true;

		joinCodeText.text = lobbyCode;
		joinCodeGroup.SetActive(true);
		title.text = lobbyTittle;
		AdjustPlayerSlots();
		return this;
	}

	private async void OnReadyClicked()
	{
		try
		{
			readyButton.interactable = !readyButton.interactable;
			leaveButton.interactable = !leaveButton.interactable;

			isReady = !isReady;
			readyButton.image.color = isReady ? readyColour : unreadyColour;
			readyButtonText.text = isReady ? "Unready" : "Ready";

			DisableReadyButtonTemp();
			//AdjustLocalReadyButton();
			await LobbyManager.Instance.SetReadyState(isReady);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public void OnVehicleSelected()
	{
		chooseVehicleViewGameObject.SetActive(false);
		readyButton.interactable = true;
	}

	private async void OnLeaveClicked()
	{
		readyButton.interactable = false;
		leaveButton.interactable = false;

		if (LobbyManager.Instance.isHost)
		{
			await LobbyManager.Instance.DeleteAnyActiveLobbyWithNotify();
		}
		else
		{
			await LobbyManager.Instance.LeaveJoinedLobby();
		}
	}

	private void AdjustPlayerSlots()
	{
		for (int i = 0; i < playerSlots.Length; i++)
		{
			if (i < activeLobbyPlayers.Count)
			{
				playerSlots[i].ConfigureAndShow(activeLobbyPlayers[i]);
			}
			else
			{
				playerSlots[i].Hide();
			}
		}
	}

	/// <summary>
	/// Adjusts the Local Player slot for our player
	/// </summary>
	public void AdjustLocalPlayerSlot()
	{
		string vehicleName = VehicleData.GetLobbyItem(int.Parse(LobbyManager.Instance.localPlayerVehicleKey)).name;
		Debug.Log($"LobbyUI :: AdjustLocalPlayerSlot() :: Showing slot {LobbyManager.Instance.localPlayerIndex} with {vehicleName}");
		playerSlots[LobbyManager.Instance.localPlayerIndex].ConfigureAndShow(LobbyManager.Instance.localPlayer);
	}
	
	public void AdjustLocalPlayerSlotReadyState()
	{
		playerSlots[LobbyManager.Instance.localPlayerIndex].SetReady(bool.Parse(LobbyManager.Instance.localPlayer.Data[PlayerDictionaryData.isReadyKey].Value));
	}

	/// <summary>
	/// Disables the Ready Button to prevent breaking Unity Lobby Rate Limits
	/// </summary>
	public void DisableReadyButtonTemp()
	{
		if (readyStateCoroutine == null)
		{
			Debug.Log("LobbyUI -> Disabled Ready Button for 1s");
			readyStateCoroutine = StartCoroutine(DisableReadyButton());
		}
		else
		{
			Debug.Log("LobbyUI -> Ready Button already off!");
		}
	}

	/// <summary>
	/// Disable the Ready Button for 1 second, so we can't spam requests
	/// </summary>
	private System.Collections.IEnumerator DisableReadyButton()
	{
		yield return new WaitForSeconds(RateLimits.Rate(RateLimits.RequestType.UpdatePlayers)); 
		readyButton.interactable = !readyButton.interactable;
		Debug.Log("LobbyUI -> Ready button wait complete. Activating");
		readyStateCoroutine = null;
	}

	/// <summary>
	/// Copy's the Relay lobby Join code to the Clipboard
	/// </summary>
	private void CopyJoinCode()
	{
		GUIUtility.systemCopyBuffer = joinCodeText.text;
	}
}