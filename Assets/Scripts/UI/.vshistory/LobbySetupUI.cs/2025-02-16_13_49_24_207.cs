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
	[SerializeField] private Color unselectedColour;
	[SerializeField] private Color selectedColour;
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
		privateButton.onClick.AddListener(delegate
		{
			isLobbyPrivate = true;
			SetLobbyNameText();
			OnLobbyAccesTypeButtonClick(new(.2F, privateButton.image, unselectedColour, selectedColour),
							  new(.2F, publicButton.image, selectedColour, unselectedColour));

		});

		publicButton.onClick.AddListener(delegate
		{
			isLobbyPrivate = false;
			SetLobbyNameText();
			OnLobbyAccesTypeButtonClick(new(.2F, publicButton.image, unselectedColour, selectedColour),
							  new(.2F, privateButton.image, selectedColour, unselectedColour));

		});

		confirmButton.onClick.AddListener(OnHostConfirmLobbyPressed);
	}

	private void Start()
	{
		isLobbyPrivate = true;
		SetLobbyNameText();
		OnLobbyAccesTypeButtonClick(new(.2F, privateButton.image, unselectedColour, selectedColour),
					      new(.2F, publicButton.image, selectedColour, unselectedColour));
	}

	private void OnLobbyAccesTypeButtonClick(params Utils.LerpGroup[] lerpGroups)
	{
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