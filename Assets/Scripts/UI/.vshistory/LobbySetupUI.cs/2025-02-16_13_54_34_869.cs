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

	private Coroutine transitionButtons;


	private void Awake()
	{
		closeButton.onClick.AddListener(OnLobbyCreationCancelled);
		privateButton.onClick.AddListener(() => OnLobbyTypeButtonClicked(true, new(.2F, privateButton.image, offColour, onColour), new(.2F, publicButton.image, onColour, offColour)));
		publicButton.onClick.AddListener(() => OnLobbyTypeButtonClicked(false, new(.2F, publicButton.image, offColour, onColour), new(.2F, privateButton.image, onColour, offColour)));
		confirmButton.onClick.AddListener(OnHostConfirmLobbyPressed);
	}

	private void Start()
	{
		OnLobbyTypeButtonClicked(true, new(.2F, privateButton.image, offColour, onColour), new(.2F, publicButton.image, onColour, offColour));
	}

	/// <summary>
	/// Sets the Lobby Access Type and animates the button colours
	/// </summary>
	/// <param name="isPrivate"></param>
	/// <param name="lerpGroups"></param>
	private void OnLobbyTypeButtonClicked(bool isPrivate, params Utils.LerpGroup[] lerpGroups)
	{
		isLobbyPrivate = isPrivate;
		SetLobbyNameText();
		if (transitionButtons != null)
			StopCoroutine(transitionButtons);

		transitionButtons = StartCoroutine(Utils.LerpImageColours(lerpGroups));
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
		Lobby lobby = await LobbyManager.Instance.CreateLobby(lobbyNameText.text, maxPlayers, GameSave.PlayerName, isLobbyPrivate, relayJoinCode);
		if (this == null) return;

		UIManager.LoadingIcon.Toggle(false);

		Toggle(false);

		UIManager.LobbyUI.Toggle(true, lobby.LobbyCode, lobby.Name);
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
	}

	public void SetLobbyNameText()
	{
		lobbyNameText.text = $"{UIManager.MainMenu.nameDisplayText.text}'s {(isLobbyPrivate ? "Private" : "Public")} Lobby";
	}
}