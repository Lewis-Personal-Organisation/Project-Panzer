using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Samples.ServerlessMultiplayerGame;
using System.Linq;
using UnityEditor;
using System.Collections;

[DisallowMultipleComponent]
public class LobbyManager : Singleton<LobbyManager>
{
	public Lobby activeLobby { get; private set; }
	public static string playerId => AuthenticationService.Instance.PlayerId;
	public List<Player> players { get; private set; }
	public Player localPlayer { get; private set; }
	public int numPlayers => players.Count;
	public bool isHost { get; private set; }
	public const string hostNameKey = "hostName";
	public const string relayJoinCodeKey = "relayJoinCode";
	public static event Action<Lobby, bool> OnLobbyChanged;
	//public static event Action OnPlayerNotInLobbyEvent;
	public static bool lobbyPreviouslyRefusedUsername = false;
	float nextHostHeartbeatTime;
	const float hostHeartbeatFrequency = 15;
	float nextUpdatePlayersTime;
	//float nextSendUpdatePlayersTime;
	bool wasGameStarted = false;

	private PlayerDictionaryData playerDictionaryData;


	public static class RateLimits
	{
		public enum RequestType
		{
			UpdatePlayers,
			UpdateLobbies,
			DeleteLobby,
			LeaveOrRemovePlayers,
		}

		private static Dictionary<RequestType, float> TypeRates = new Dictionary<RequestType, float>
		{
			{ RequestType.UpdatePlayers, 1.1F },
			{ RequestType.UpdateLobbies, 1.1F },
			{ RequestType.DeleteLobby, 2.1F },
			{ RequestType.LeaveOrRemovePlayers, 5.1F},
		};

		/// <summary>
		/// Returns the time to wait for lobby request type
		/// </summary>
		public static float Rate(RequestType type) => TypeRates[type];

		/// <summary>
		/// Use to override the default limit of a Request Type
		/// </summary>
		/// <param name="overrideType"></param>
		/// <param name="newLimit"></param>
		public static void OverrideRate(RequestType overrideType, float newLimit) => TypeRates[overrideType] = newLimit;
	}

	public class PlayerDictionaryData
	{
		public PlayerDictionaryData(string playerName, bool isPlayerReady, int vehicleIndex)
		{
			this.playerName = playerName;
			this.isReady = isPlayerReady;
			this.lobbyVehicleIndex = vehicleIndex;
		}

		public const string nameKey = "playerName";
		public string playerName;
		public const string isReadyKey = "isReady";
		public bool isReady = false;
		public const string vehicleIndexKey = "vehicleIndex";
		public int lobbyVehicleIndex = -1;
	}

	ILobbyEvents activeLobbyEvents;


	new private void Awake()
	{
		base.Awake();
	}

    async void Update()
    {
		try
		{
			if (activeLobby != null && !wasGameStarted)
			{
				if (isHost && Time.realtimeSinceStartup >= nextHostHeartbeatTime)
				{
					await PeriodicHostHeartbeat();

					// Exit this update now so we'll only ever update 1 item (heartbeat or lobby changes) in 1 Update().
					return;
				}

				if (Time.realtimeSinceStartup >= nextUpdatePlayersTime)
				{
					await PeriodicGetUpdatedLobby();
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	private async Task PeriodicHostHeartbeat()
	{
		try
		{
			// Set next heartbeat time before calling Lobby Service since next update could also trigger a
			// heartbeat which could cause throttling issues.
			nextHostHeartbeatTime = Time.realtimeSinceStartup + hostHeartbeatFrequency;

			await LobbyService.Instance.SendHeartbeatPingAsync(activeLobby.Id);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	private async Task PeriodicGetUpdatedLobby()
	{
		try
		{
			// Set next update time before calling Lobby Service since next update could also trigger an
			// update which could cause throttling issues.
			nextUpdatePlayersTime = Time.realtimeSinceStartup + RateLimits.Rate(RateLimits.RequestType.UpdatePlayers);
			//Debug.Log($"LobbyManager :: Updating Lobby ({Time.realtimeSinceStartup}s). Interval -> {RateLimits.Rate(RateLimits.RequestType.UpdatePlayers)}s");
			var updatedLobby = await LobbyService.Instance.GetLobbyAsync(activeLobby.Id);

			if (this == null)
			{
				return;
			}

			UpdateLobby(updatedLobby);
		}

		// Handle lobby no longer exists (host canceled game and returned to main menu).
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
		{
			if (this == null) return;

			// Lobby has closed
			//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
			//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.LobbyClosed);
			Debug.Log("Lobby Not Found or Closed. Returning to Main Menu");
			OnPlayerNotInLobby();
		}

		// Handle player no longer allowed to view lobby (host booted player so player is no longer in the lobby).
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.Forbidden)
		{
			if (this == null) return;

			//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
			//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);

			OnPlayerNotInLobby();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
	//async Task PeriodicSendUpdatedLobby()
	//{
	//	try
	//	{
	//		Debug.Log($"Trying to send updated Lobby. Next Update in {updatePlayersFrequency}s");
	//		// Set next update time before calling Lobby Service since next update could also trigger an
	//		// update which could cause throttling issues.
	//		nextSendUpdatePlayersTime = Time.realtimeSinceStartup + updatePlayersFrequency;

	//		var options = new UpdatePlayerOptions();
	//		options.Data = CreatePlayerDictionary();

	//		var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(activeLobby.Id, playerId, options);

	//		if (this == null)
	//		{
	//			return;
	//		}

	//		UpdateLobby(updatedLobby);
	//	}
	//	// Handle lobby no longer exists (host canceled game and returned to main menu).
	//	catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
	//	{
	//		if (this == null) return;

	//		// Lobby has closed
	//		//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
	//		//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.LobbyClosed);
	//		Debug.Log("Lobby Not Found or Closed. Returning to Main Menu");
	//		OnPlayerNotInLobby();
	//	}

	//	// Handle player no longer allowed to view lobby (host booted player so player is no longer in the lobby).
	//	catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.Forbidden)
	//	{
	//		if (this == null) return;

	//		//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
	//		//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);

	//		OnPlayerNotInLobby();
	//	}
	//	catch (Exception e)
	//	{
	//		Debug.LogException(e);
	//	}

	//	updatePlayerAsyncPending = false;
	//}

	void UpdateLobby(Lobby updatedLobby)
	{
		// Since this is called after an await, ensure that the Lobby wasn't closed while waiting.
		if (activeLobby == null || updatedLobby == null)
		{
			return;
		}

		if (DidPlayersChange(activeLobby.Players, updatedLobby.Players))
		{
			//Debug.Log("Update Lobby :: Players Changed - Lobby Updated!");
			activeLobby = updatedLobby;
			players = activeLobby?.Players;
			CacheLocalPlayer();

			// If our player exists, check if all other players are ready
			if (updatedLobby.Players.Exists(player => player.Id == playerId))
			{
				var isGameReady = AllPlayersReady(updatedLobby);

				// Trigger event with value (This starts the game if all players are ready)
				OnLobbyChanged?.Invoke(updatedLobby, isGameReady);
			}
			else
			{
				Debug.Log("Update Lobby : Player Kicked");
				ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
					ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);

				OnPlayerNotInLobby();
			}
		}
	}

	/// <summary>
	/// If Lobby is not null, remove ourself from it
	/// </summary>
	public void LeaveLobbyOnQuit()
	{
		if (activeLobby != null)
		{
			LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, playerId);
		}
	}

	public void OnPlayerNotInLobby()
	{
		if (activeLobby != null)
		{
			activeLobby = null;
			UIManager.LobbyUI.LeaveLobby();
			StartCoroutine(ShutdownNetworkAndReturnToMainMenu());
		}
	}
	private IEnumerator ShutdownNetworkAndReturnToMainMenu()
	{
		yield return StartCoroutine(SessionManager.Instance.IShutdownNetworkClient());
		UIManager.MainMenu.Toggle(true);
		UIManager.LobbySetupMenu.ToggleLobbyCreationInteractables(true);
		Debug.Log("LobbyUI :: Re-enabled buttons after shutdown");
	}

	static bool DidPlayersChange(List<Player> oldPlayers, List<Player> newPlayers)
	{
		if (oldPlayers.Count != newPlayers.Count)
		{
			Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Player Count Changed");
			return true;
		}

		for (int i = 0; i < newPlayers.Count; i++)
		{
			if (oldPlayers[i].Id != newPlayers[i].Id ||
				oldPlayers[i].Data[PlayerDictionaryData.isReadyKey].Value != newPlayers[i].Data[PlayerDictionaryData.isReadyKey].Value)
			{
				Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Player ID/Ready State Changed");
				return true;
			}

			if (oldPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value != newPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value)
			{
				Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Vehicle Index Changed");
				return true;
			}
		}

		return false;
	}

	static bool AllPlayersReady(Lobby lobby)
	{
		if (lobby.Players.Count <= 1)
		{
			return false;
		}

		foreach (var player in lobby.Players)
		{
			var isReady = bool.Parse(player.Data[PlayerDictionaryData.isReadyKey].Value);
			if (!isReady)
			{
				return false;
			}
		}

		return true;
	}

	public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string hostName,
			bool isPrivate, string relayJoinCode)
	{
		try
		{
			playerDictionaryData = new(hostName, false, -1);
			isHost = true;
			wasGameStarted = false;

			await DeleteAnyActiveLobbyWithNotify();
			if (this == null) return default;

			var options = new CreateLobbyOptions
			{
				IsPrivate = isPrivate,
				Data = new Dictionary<string, DataObject>
				{
					{ hostNameKey, new DataObject(DataObject.VisibilityOptions.Public, hostName) },
					{ relayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) },
				},

				Player = CreatePlayerData()
			};

			activeLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

			// Callbacks
			LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
			//callbacks.LobbyEventConnectionStateChanged += OnConnectionStateChanged;
			//callbacks.PlayerJoined += OnPlayersJoinedLobby;
			//callbacks.PlayerLeft += OnPlayersLeftLobby;
			callbacks.LobbyChanged += OnLobbyChangedNotif;
			//callbacks.LobbyDeleted +=
			//	callbacks.KickedFromLobby
			try
			{
				activeLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(activeLobby.Id, callbacks);
			}
			catch (LobbyServiceException ex)
			{
				switch (ex.Reason)
				{
					case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{activeLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
					case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
					case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
					default: throw;
				}
			}

			if (this == null) return default;

			players = activeLobby?.Players;
			LogLobbyCreation(activeLobby);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}

		return activeLobby;
	}

	public async Task<Lobby> JoinPrivateLobby(string lobbyJoinCode, string playerName)
	{
		try
		{
			await PrepareToJoinLobby(playerName);
			if (this == null) return default;

			var options = new JoinLobbyByCodeOptions();
			options.Player = CreatePlayerData();

			Debug.Log($"Joining lobby with Code {lobbyJoinCode}");
			activeLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyJoinCode, options);
			if (this == null) return default;

			players = activeLobby?.Players;
		}
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
		{
			if (this == null) return null;

			activeLobby = null;

			throw;
		}
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyFull)
		{
			if (this == null) return null;

			activeLobby = null;

			throw;
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}

		return activeLobby;
	}

	async Task PrepareToJoinLobby(string playerName)
	{
		isHost = false;
		this.wasGameStarted = false;
		this.playerDictionaryData = new(playerName, false, -1);

		if (activeLobby != null)
		{
			Debug.Log("Already in a lobby when attempting to join so leaving old lobby.");
			await LeaveJoinedLobby();
		}
	}

	public async Task LeaveJoinedLobby()
	{
		try
		{
			await RemovePlayer(playerId);
			if (this == null) return;

			OnPlayerNotInLobby();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public async Task RemovePlayer(string playerId)
	{
		try
		{
			if (activeLobby != null)
			{
				await LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, playerId);
				Debug.Log("Removed Player");
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	//public Player[] GetLobbyPlayers()
	//{
	//	if (activeLobby != null)
	//	{
	//		return activeLobby.Players.ToArray();
	//	}
	//	else
	//	{
	//		return null;
	//	}
	//}

	public void LogLobbyPlayers()
	{
		if (activeLobby.Players == null)
		{
			Debug.Log("Players are null. Returning");
			return;
		}

		string lobbyPlayerNames = "Player(s) ";
		for (int i = 0; i < activeLobby.Players.Count; i++)
		{
			lobbyPlayerNames += $"'{players[i].Data[PlayerDictionaryData.nameKey].Value}'";

			if (i == activeLobby.Players.Count - 1)
				lobbyPlayerNames += " are present";
			else
				lobbyPlayerNames += ", ";
		}

		Debug.Log($"LobbyManager :: {lobbyPlayerNames}");
	}

	public async Task DeleteAnyActiveLobbyWithNotify()
	{
		try
		{
			if (activeLobby != null && isHost)
			{
				await LobbyService.Instance.DeleteLobbyAsync(activeLobby.Id);
				if (this == null) return;

				OnPlayerNotInLobby();
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	Player CreatePlayerData()
	{
		var player = new Player();
		player.Data = CreatePlayerDictionary();

		return player;
	}

	public Dictionary<string, PlayerDataObject> CreatePlayerDictionary()
	{
		var playerDictionary = new Dictionary<string, PlayerDataObject>
			{
				{ PlayerDictionaryData.nameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerDictionaryData.playerName) },
				{ PlayerDictionaryData.vehicleIndexKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerDictionaryData.lobbyVehicleIndex.ToString()) },
				{ PlayerDictionaryData.isReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerDictionaryData.isReady.ToString()) },
			};

		return playerDictionary;
	}

	public static void LogLobbyCreation(Lobby lobby)
	{
		if (lobby is null)
		{
			Debug.Log("No active lobby.");
			return;
		}

		var lobbyData = lobby.Data.Select(kvp => $"{kvp.Key} is {kvp.Value.Value}");
		var lobbyDataStr = string.Join(", ", lobbyData);

		Debug.Log($"LobbyManager :: Lobby Named:{lobby.Name}, " +
			$"Players:{lobby.Players.Count}/{lobby.MaxPlayers}, " +
			$"IsPrivate:{lobby.IsPrivate}, " +
			$"IsLocked:{lobby.IsLocked}, " +
			$"LobbyCode:{lobby.LobbyCode}, " +
			$"Id:{lobby.Id}, " +
			$"Created:{lobby.Created}, " +
			$"HostId:{lobby.HostId}, " +
			$"EnvironmentId:{lobby.EnvironmentId}, " +
			$"Upid:{lobby.Upid}, " +
			$"Lobby.Data: {lobbyDataStr}");

		Instance.LogLobbyPlayers();
	}
	
	public async Task SetReadyState(bool isReady)
	{
		try
		{
			if (activeLobby == null)
			{
				Debug.Log("Attempting to toggle ready state when not already in a lobby.");
				return;
			}

			playerDictionaryData.isReady = isReady;

			var lobbyId = activeLobby.Id;

			var options = new UpdatePlayerOptions();
			options.Data = CreatePlayerDictionary();

			var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
			if (this == null) return;

			UpdateLobby(updatedLobby);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}


	public async Task SwapLobbyVehicle(int index)
	{
		try
		{
			if (activeLobby == null)
			{
				Debug.Log("Attempting to swap vehicle when not already in a lobby.");
				return;
			}

			if (playerDictionaryData.lobbyVehicleIndex == index)
			{
				Debug.Log($"Vehicle Choice :: Player select same vehicle, no need to update Network");
				return;
			}

			playerDictionaryData.lobbyVehicleIndex = index;
			Debug.Log($"Updated Lobby Vehicle index for Sync");

			var lobbyId = activeLobby.Id;

			var options = new UpdatePlayerOptions();
			options.Data = CreatePlayerDictionary();

			var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
			if (this == null) return;

			UpdateLobby(updatedLobby);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Asynchronously await for some amount of time
	/// </summary>
	/// <param name="seconds"></param>
	//private async Task WaitForSecondsAsync(float seconds)
	//{
	//	await Task.Delay(TimeSpan.FromSeconds(seconds));
	//}

	/// <summary>
	/// Check if players connected already use our name
	/// Since we are already connected to a lobby at this point, we must check for 2 matches instead of 1
	/// </summary>
	public bool PlayerNameCheck(string ownerName)
	{
		int matches = 0;
		for (int i = 0; i < activeLobby.Players.Count; i++)
		{
			//Debug.Log($"Name Check: Found player '{players[i].Data[PlayerDictionaryData.playerNameKey].Value}'");
			if (ownerName == players[i].Data[PlayerDictionaryData.nameKey].Value)
			{
				matches++;
				//Debug.Log($"Name Check: Name Matched! {players[i].Data[PlayerDictionaryData.playerNameKey].Value} + {ownerName}, Count {matches}");

				if (matches == 2)
					return false;
			}
		}
		return true;
	}
	//public void OnConnectionStateChanged(LobbyEventConnectionState newState)
	//{
	//	if (isHost)
	//	{
	//		Debug.Log($"LobbyManager (Host) :: OnConnectionStateChanged :: Lobby Connection State is {newState}");
	//	}
	//}
	//public void OnPlayersJoinedLobby(List<LobbyPlayerJoined> newPlayers)
	//{
	//	if (isHost)
	//	{
	//		for (int i = 0; i < newPlayers.Count; i++)
	//		{
	//			Debug.Log($"LobbyManager (Host) :: OnPlayersJoinedLobby :: Player '{newPlayers[i].Player.Data[PlayerDictionaryData.nameKey].Value}' joined!");
	//		}
	//	}
	//}
	//public void OnPlayersLeftLobby(List<int> leftPlayers)
	//{
	//	if (isHost)
	//	{
	//		for (int i = 0; i < leftPlayers.Count; i++)
	//		{
	//			Debug.Log($"LobbyManager (Host) :: OnPlayersLeftLobby :: Player '{leftPlayers[i]}' Left!");
	//		}
	//	}
	//}
	public void OnLobbyChangedNotif(ILobbyChanges changes)
	{

		if (changes.LobbyDeleted)
		{

		}
		else
		{
			changes.ApplyToLobby(activeLobby);

			if (changes.PlayerJoined.Changed)
			{
				Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Player Joined");
			}
			if (changes.PlayerLeft.Changed)
			{
				Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Player Left");
			}
			if ( changes.)
		}
	}


	/// <summary>
	/// Attempts to join a private lobby using the specified Join Code. 
	/// Also ensures Player names are unique. If not, requires user to re-enter name
	/// </summary>
	public async void JoinPrivateLobbyAsClient(string joinCode, string playername)
	{
		try
		{
			UIManager.LoadingIcon.ShowWithText("Joining Lobby...");
			await SessionManager.Instance.InitialiseUnityServices();
			Lobby joinedLobby = await Instance.JoinPrivateLobby(joinCode, playername);

			if (this == null)
			{
				Debug.Log("Null. Returning");
				return;
			}

			if (joinedLobby == null)
			{
				Debug.Log("Failed to Join Private Lobby");
				// Could return player to main menu here
				return;
			}

			UIManager.LoadingIcon.Toggle(false);
			Debug.Log($"Checking Name {playername}");
			Instance.LogLobbyPlayers();

			if (Instance.activeLobby == null)
				return;

			bool nameCheckPassed = Instance.PlayerNameCheck(playername);

			if (nameCheckPassed)
			{
				lobbyPreviouslyRefusedUsername = false;
				Debug.Log("Name Check: Passed");
				Instance.LogLobbyPlayers();
				await OpenLobby(joinedLobby);
			}
			else
			{
				lobbyPreviouslyRefusedUsername = true;
				Debug.Log("Name Check: Failed. Leaving Lobby...");
				await Instance.LeaveJoinedLobby();
				Debug.Log("Name Check: Left Lobby. Set a unique name and rejoin!");
				UIManager.TextInputGroup.ToggleInputTextGroup(true, TextSubmissionContext.PlayerName);
				UIManager.MainMenu.Toggle(false);
				UIManager.TextInputGroup.TogglePasteButton(false);
			}
		}
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
		{
			Debug.Log("Failed to Join Private Lobby: Invalid Code");
		}
		catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyFull)
		{
			Debug.Log("Failed to Join Private Lobby: Lobby Full");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Shows the Lobby UI once a lobby is joined
	/// </summary>
	async Task OpenLobby(Lobby lobbyJoined)
	{
		Debug.Log("Lobby Data Retrieved. Initializing Client");

		try
		{
			await SessionManager.Instance.InitializeRelayClient(lobbyJoined);

			if (this == null)
			{
				Debug.Log("Null. Returning");
				return;
			}

			UIManager.MainMenu.Toggle(false);
			UIManager.LobbyUI.Toggle(true, lobbyJoined.LobbyCode, lobbyJoined.Name);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Caches the Local Player for quick/efficient access
	/// </summary>
	/// <returns></returns>
	public void CacheLocalPlayer()
	{
		if (activeLobby == null)
			return;

		for (int i = 0; i < activeLobby.Players.Count; i++)
		{
			if (playerId == activeLobby.Players[i].Id)
				localPlayer = activeLobby.Players[i];
		}

		return;
	}
}