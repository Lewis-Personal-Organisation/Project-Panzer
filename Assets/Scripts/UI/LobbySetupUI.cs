using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbySetupUI : Panel
{
	[Header("Host Lobby")]
	[SerializeField] private TextMeshProUGUI lobbyNameText;
	[SerializeField] private Button closeButton;
	[SerializeField] private Color closeButtonEnabledColour;
	[SerializeField] private Color closeButtonDisabledColour;
	[SerializeField] private Button privateButton;
	[SerializeField] private Color offColour;
	[SerializeField] private Color onColour;
	[SerializeField] private Button publicButton;
	[SerializeField] private Button confirmButton;

	[SerializeField] private bool isLobbyPrivate;
	public int maxPlayers = 4;
	public string relayJoinCode = string.Empty;
	[SerializeField] UnityTransport unityTransport;
	private bool networkManagerInitialised = false;


	private void Awake()
	{
		onPushAction.AddListener(privateButton.onClick.Invoke);
		onPopAction.AddListener(privateButton.onClick.Invoke);
		
		closeButton.onClick.AddListener(() => UIManager.PopPanel());

		privateButton.onClick.AddListener(delegate
		{
			isLobbyPrivate = true;
			SetLobbyNameText();
			privateButton.image.TweenGraphicColor(onColour, .2F);
			publicButton.image.TweenGraphicColor(offColour, .2F);
		});

		publicButton.onClick.AddListener(delegate
		{
			isLobbyPrivate = false;
			SetLobbyNameText();
			publicButton.image.TweenGraphicColor(onColour, .2F);
			privateButton.image.TweenGraphicColor(offColour, .2F);
		});

		confirmButton.onClick.AddListener(OnHostConfirmLobbyPressed);
	}

	/// <summary>
	/// Returns the Player to the Main Menu
	/// </summary>
	// private void OnLobbyCreationCancelled()
	// {
	// 	UIManager.Instance.PopPanel();
	// }

	/// <summary>
	/// Toggles interactable buttons for Lobby creation UI
	/// </summary>
	public void ToggleLobbyCreationInteractables(bool state)
	{
		privateButton.interactable = state;
		publicButton.interactable = state;
		confirmButton.interactable = state;
	}

	/// <summary>
	/// Requests Lobby from Unity Relay (Initialises Unity Services if required).
	/// Populates the Join Code UI for Copying
	/// </summary>
	private async void OnHostConfirmLobbyPressed()
	{
		closeButton.image.color = closeButtonDisabledColour;
		closeButton.enabled = false;

		ToggleLobbyCreationInteractables(false);
		UIManager.PushPanel(UIManager.LoadingIcon.SetText("Creating Lobby..."));

		await SessionManager.Instance.InitialiseAndAuthenticatePlayer();

		// Get a relayJoinCode for our server allocation
		relayJoinCode = await InitialiseHostWithRelay(maxPlayers);
		if (this == null) return;
		
		// Create lobby with Relay
		Lobby lobby = await LobbyManager.Instance.CreateLobby(lobbyNameText.text, maxPlayers, GameSave.PlayerName, isLobbyPrivate, relayJoinCode);
		if (this == null) return;

		closeButton.image.color = closeButtonEnabledColour;
		closeButton.enabled = true;
		
		UIManager.PopAndPush(2, UIManager.LobbyUI.Prepare(lobby.LobbyCode, lobby.Name));
	}

	/// <summary>
	/// Returns a Join Code after requesting Unity Relay to allocate/reserve space on a server
	/// Also starts the Player as Host
	/// </summary>
	private async Task<string> InitialiseHostWithRelay(int maxPlayerCount)
	{
		Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayerCount);
		string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
		
		NetworkEndPoint endPoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
		
		string ipAddress = endPoint.Address.Split(':')[0];
		unityTransport.SetHostRelayData(ipAddress, endPoint.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, false);

		NetworkManager.Singleton.StartHost();
		networkManagerInitialised = true;
		return joinCode;
	}

	// public override void Toggle(bool activeState)
	// {
	// 	base.Toggle(activeState);
	// 	privateButton.onClick.Invoke();
	// }

	public Panel Prepare()
	{
		lobbyNameText.text = $"{UIManager.MainMenu.nameDisplayText.text}'s {(isLobbyPrivate ? "Private" : "Public")} Lobby";
		return this;
	}
	
	public void SetLobbyNameText()
	{
		lobbyNameText.text = $"{UIManager.MainMenu.nameDisplayText.text}'s {(isLobbyPrivate ? "Private" : "Public")} Lobby";
	}
}