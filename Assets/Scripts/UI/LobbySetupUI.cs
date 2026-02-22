using System;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
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
	public Button confirmButton;

	[SerializeField] private bool isLobbyPrivate;
	public int maxPlayers = 4;
	public string relayJoinCode = string.Empty;
	[SerializeField] UnityTransport unityTransport;


	private void Awake()
	{
		onPushAction.AddListener(privateButton.onClick.Invoke);
		onPopAction.AddListener(privateButton.onClick.Invoke);
		
		closeButton.onClick.AddListener(() => UIManager.PopAllAndPush(UIManager.MainMenu));

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

		// Shut down the Net Manager to clean up its state
		if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
		{
			NetworkManager.Singleton.Shutdown();
			
			// Wait until the Net Manager is shut down, to clear the transport data fully.
			// Not doing this means residual old data could exist, resulting in errors
			while (NetworkManager.Singleton.ShutdownInProgress)
			{
				await Task.Yield();
			}
        
			Debug.Log("Network shutdown complete, proceeding to create new host");
		}
		
		// Get a relayJoinCode for our server allocation
		relayJoinCode = await InitializeHostWithRelay(maxPlayers);
		if (this == null)
		{
			Debug.LogError("OnHostConfirmLobbyPressed :: We are null - A");
		}
		
		// Create lobby with Relay
		Lobby lobby = await LobbyManager.Instance.CreateLobby(lobbyNameText.text, maxPlayers, GameSave.PlayerName, isLobbyPrivate, relayJoinCode);
		if (this == null)
		{
			Debug.LogError("OnHostConfirmLobbyPressed :: We are null - B");
		}

		closeButton.image.color = closeButtonEnabledColour;
		closeButton.enabled = true;

		// Copy code to buffer
		GUIUtility.systemCopyBuffer = lobby.LobbyCode;
		
		UIManager.PopAndPush(2, UIManager.LobbyUI.Prepare(lobby.LobbyCode, lobby.Name));
	}

	/// <summary>
	/// Returns a Join Code after requesting Unity Relay to allocate/reserve space on a server
	/// Also starts the Player as Host
	/// </summary>
	private async Task<string> InitializeHostWithRelay(int maxPlayerCount)
	{
		try
		{
			await SessionManager.Instance.InitialiseAndAuthenticatePlayer();
		
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayerCount);
			string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			// Debug.Log($"LobbySetupUI :: InitialiseHostWithRelay :: LOBBY JOIN CODE: '{joinCode}'");
		
			// Use RelayServerData to properly package all relay information
			RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
		
			SessionManager.Instance.unityTransport.SetRelayServerData(relayServerData);

			NetworkManager.Singleton.StartHost();
			LobbyDebugViewer.Instance.SetAllocationID(allocation);
			return joinCode;
		}
		catch (RelayServiceException e)
		{
			Debug.Log(e);
			throw;
		}
	}

	public Panel Prepare()
	{
		lobbyNameText.text = $"{UIManager.MainMenu.nameDisplayText.text}'s {(isLobbyPrivate ? "Private" : "Public")} Lobby";
		return this;
	}
	
	/// <summary>
	/// Sets the lobby name
	/// </summary>
	public void SetLobbyNameText()
	{
		lobbyNameText.text = $"{UIManager.MainMenu.nameDisplayText.text}'s {(isLobbyPrivate ? "Private" : "Public")} Lobby";
	}
}