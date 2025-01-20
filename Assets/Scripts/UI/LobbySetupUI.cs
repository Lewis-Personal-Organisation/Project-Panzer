using UnityEngine.UI;
using TMPro;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Netcode;
using System.Threading.Tasks;

public class LobbySetupUI : Panel
{
	[Header("Host Lobby")]
	[SerializeField] private TextMeshProUGUI gameNameText;
	[SerializeField] private Button closeButton;
	[SerializeField] private Button privateButton;
	[SerializeField] private Button publicButton;
	[SerializeField] private Button confirmButton;

	[SerializeField] private bool isLobbyPrivate;
	public int maxPlayers = 4;
	public string relayJoinCode = string.Empty;
	[SerializeField] UnityTransport unityTransport;
	private bool networkManagerInitialised = false;


	private void Awake()
	{
		closeButton.onClick.AddListener(OnLobbyCreationCancelled);
		privateButton.onClick.AddListener(delegate { isLobbyPrivate = true; });
		publicButton.onClick.AddListener(delegate { isLobbyPrivate = false; });
		confirmButton.onClick.AddListener(OnHostConfirmLobbyPressed);
	}

	/// <summary>
	/// Returns the Player to the Main Menu
	/// </summary>
	private void OnLobbyCreationCancelled()
	{
		Debug.Log("Showing Main Menu");
		UIManager.MainMenu.Toggle(true);
		Toggle(false);
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
		ToggleLobbyCreationInteractables(false);
		UIManager.LoadingIcon.ShowWithText("Creating Lobby...");

		await SessionManager.Instance.InitialiseUnityServices();

		// Start Hosting using Relay
		relayJoinCode = await InitialiseHostWithRelay(maxPlayers);
		if (this == null) return;

		// Create lobby with Relay
		Lobby lobby = await LobbyManager.Instance.CreateLobby(gameNameText.text, maxPlayers, GameSave.PlayerName, isLobbyPrivate, relayJoinCode);
		if (this == null) return;

		UIManager.LoadingIcon.Toggle(false);

		Toggle(false);

		// Configures the LobbyUI Vehicle Arrow Buttons
		//UIManager.LobbyUI.AssignVehicleUIArrowButtons(0);
		UIManager.LobbyUI.Toggle(true, lobby.LobbyCode);
	}

	/// <summary>
	/// Returns a Join Code after requesting Unity Relay to allocate/reserve space on a server
	/// Also starts the Player as Host
	/// </summary>
	public async Task<string> InitialiseHostWithRelay(int maxPlayerCount)
	{
		Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayerCount);
		var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
		NetworkEndPoint endPoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4,
			(ushort)allocation.RelayServer.Port);
		var ipAddress = endPoint.Address.Split(':')[0];
		unityTransport.SetHostRelayData(ipAddress, endPoint.Port,
			allocation.AllocationIdBytes, allocation.Key,
			allocation.ConnectionData, false);

		NetworkManager.Singleton.StartHost();
		networkManagerInitialised = true;
		return joinCode;
	}

	public override void Toggle(bool activeState)
	{
		base.Toggle(activeState);
		privateButton.Select();
		//isLobbyPrivate = false;
	}

	public void SetLobbyNameText(string name)
	{
		gameNameText.text = $"{name}'s Lobby";
	}
}