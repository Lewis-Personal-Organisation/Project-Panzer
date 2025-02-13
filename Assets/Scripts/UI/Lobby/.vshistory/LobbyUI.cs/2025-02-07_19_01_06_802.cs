using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
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

	private List<Unity.Services.Lobbies.Models.Player> activeLobbyPlayers => LobbyManager.Instance.activeLobby.Players;

	[SerializeField] private PlayerSlot[] playerSlots = new PlayerSlot[4];
	[SerializeField] private VehicleSlot[] vehicleSlots = new VehicleSlot[5];

	private void Awake()
	{
		// Setup Vehicle Select slots
		for (int i = 0; i < vehicleSlots.Length; i++)
		{
			vehicleSlots[i].Set(vehicleSlots[i].dataIndex);
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
		LobbyManager.Instance.LeaveLobbyOnQuit();
	}

	void OnLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		AdjustPlayerSlots();
		AdjustLocalReadyButton();

		if (LobbyManager.Instance.isHost)
		{
			OnHostLobbyChanged(updatedLobby, isGameReady);
		}
		else
		{
			OnJoinLobbyChanged(updatedLobby, isGameReady);
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

	public void OnHostLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		//Debug.Log("OnLobbyChanged :: Host");
		//sceneView.SetHostLobbyPlayers(updatedLobby.Players);

		if (isGameReady)
		{
			// GAME IS READY TO START
			//NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
		}

		//LobbyManager.instance.LogLobbyPlayers();
	}

	public void OnJoinLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		Debug.Log("OnLobbyChanged :: Client");
		//sceneView.SetJoinLobbyPlayers(updatedLobby.Players);
	}

	public void LeaveLobby()
	{
		Toggle(false); 
		Debug.Log($"This player left the Lobby and returned to main menu");
	}


	public void Toggle(bool activeState, string lobbyCode)
	{
		base.Toggle(activeState);
		readyButton.interactable = true;
		leaveButton.interactable = true;

		joinCodeText.text = lobbyCode;
		joinCodeGroup.SetActive(true);
		SetLobbyTitle();
		AdjustPlayerSlots();
	}

	private void SetLobbyTitle()
	{
		title.text = LobbyManager.Instance.isHost ? "Host Game Lobby" : "Game Lobby";
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
			await LobbyManager.Instance.SetReadyState(isReady);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
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

	public void AdjustPlayerSlots()
	{
		for (int i = 0; i < playerSlots.Length; i++)
		{
			if (i < activeLobbyPlayers.Count)
			{
				Debug.Log($"Showing with {activeLobbyPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value}");
				playerSlots[i].ConfigureOpenVehicleSelectionButton(LobbyManager.playerId == activeLobbyPlayers[i].Id);
				playerSlots[i].Show(activeLobbyPlayers[i].Data[PlayerDictionaryData.nameKey].Value, int.Parse(activeLobbyPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value));
				playerSlots[i].SetReady(bool.Parse(activeLobbyPlayers[i].Data[PlayerDictionaryData.isReadyKey].Value));
			}
			else
			{
				playerSlots[i].Hide();
			}
		}
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