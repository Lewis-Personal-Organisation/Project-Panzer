using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

	[Header("Join Code UI")]
	[SerializeField] private GameObject joinCodeGroup;
	[SerializeField] private Button joinCodeCopyButton;
	[SerializeField] private TextMeshProUGUI joinCodeText;

	private List<Unity.Services.Lobbies.Models.Player> activeLobbyPlayers => LobbyManager.Instance.activeLobby.Players;

	[SerializeField] private PlayerSlot[] playerSlots = new PlayerSlot[4];

	[System.Serializable]
	private class PlayerSlot
	{
		[System.Serializable]
		public class VehicleLobbyUI
		{
			[SerializeField] private TextMeshProUGUI vehicleName;
			[SerializeField] private TextMeshProUGUI vehicleType;
			[SerializeField] private Image vehicleImage;
			[SerializeField] private Image firepowerImage;
			[SerializeField] private Image mobilityImage;
			[SerializeField] private Image defenceImage;
			[SerializeField] private Button leftArrow;
			[SerializeField] private Button rightArrow;

			public int activeVehicleIndex = 0;

			public void Set(int index)
			{
				//if (this.vehicleName.text == VehicleData.Instance.GetItem(activeVehicleIndex).name) return;

				VehicleLobbyData vData = VehicleData.GetItem(index);
				this.vehicleName.text = vData.name;
				this.vehicleType.text = vData.type.ToString();
				this.vehicleImage.sprite = vData.icon;
				this.firepowerImage.fillAmount = vData.firepower;
				this.mobilityImage.fillAmount = vData.mobility;
				this.defenceImage.fillAmount = vData.defence;

				Debug.Log($"Loaded Vehicle {this.vehicleName.text}");
			}

			public void SetUIButtons(bool isOwner)
			{
				leftArrow.onClick.RemoveAllListeners();
				rightArrow.onClick.RemoveAllListeners();

				if (isOwner)
				{
					leftArrow.onClick.AddListener(() => OnVehicleUIArrowClicked(-1));
					rightArrow.onClick.AddListener(() => OnVehicleUIArrowClicked(1));
				}

				leftArrow.gameObject.SetActive(isOwner);
				rightArrow.gameObject.SetActive(isOwner);
			}

			public void ClearUIButtons()
			{
				leftArrow.onClick.RemoveAllListeners();
				leftArrow.gameObject.SetActive(false);
				rightArrow.onClick.RemoveAllListeners();
				rightArrow.gameObject.SetActive(false);
			}

			public async void OnVehicleUIArrowClicked(int direction)
			{
				int newValue = activeVehicleIndex + direction;
				activeVehicleIndex = (newValue == VehicleData.ItemCount() ? 0 : (newValue < 0 ? VehicleData.ItemCount() - 1 : newValue));
				//if (activeVehicleIndex + direction == VehicleData.ItemCount())
				//	activeVehicleIndex = 0;
				//else if (direction + activeVehicleIndex < 0)
				//	activeVehicleIndex = VehicleData.ItemCount() - 1;
				//else
				//	activeVehicleIndex += direction;

				Set(activeVehicleIndex);

				try
				{
					leftArrow.interactable = false;
					rightArrow.interactable = false;

					// Should only be called on the owning object
					await LobbyManager.Instance.SwapLobbyVehicle(activeVehicleIndex);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				finally
				{
					if (this != null)
					{
						Debug.Log($"Lobby Vehicle Swap request complete. Enabling controls");
						leftArrow.interactable = true;
						rightArrow.interactable = true;
					}
				}
			}
		}

		[SerializeField] private GameObject parent;
		[SerializeField] private TextMeshProUGUI playerNameTitle;
		[SerializeField] private GameObject readyGameObject;
		public VehicleLobbyUI vehicleUI;

		public bool IsFree() => !parent.activeInHierarchy;

		public void Show(string playerName, int vehicleIndex)
		{
			playerNameTitle.text = playerName;
			vehicleUI.Set(vehicleIndex);
			parent.SetActive(true);
		}

		public void Hide()
		{
			playerNameTitle.text = string.Empty;
			parent.SetActive(false);
		}

		/// <summary>
		/// Enable/Disabled buttons events for if we own these buttons
		/// </summary>
		public void ConfigureVehicleChoiceButtons(bool isOwner)
		{
			Debug.Log($"Refreshing Buttons for {UIManager.MainMenu.nameDisplayText.text}!");
			vehicleUI.SetUIButtons(isOwner);
		}

		public void SetReady(bool isReady) => readyGameObject.SetActive(isReady);
	}


	private void Awake()
	{
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
				playerSlots[i].ConfigureVehicleChoiceButtons(LobbyManager.playerId == activeLobbyPlayers[i].Id);
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
		yield return new WaitForSeconds(RateLimits.RatePerSecond(RateLimits.RequestType.UpdatePlayers)); 
		readyButton.interactable = !readyButton.interactable;
		leaveButton.interactable = !leaveButton.interactable;
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