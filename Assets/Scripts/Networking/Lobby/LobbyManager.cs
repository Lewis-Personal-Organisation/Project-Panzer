using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class LobbyManager : Singleton<LobbyManager>
{
	public Lobby activeLobby { get; private set; }
	public static string playerId => AuthenticationService.Instance.PlayerId;
	public List<Player> players { get; private set; }
	
	public Player localPlayer { get; private set; }
	public int localPlayerIndex { get; private set; }
	public bool isHost { get; private set; }
	public const string hostNameKey = "hostName";
	public const string relayJoinCodeKey = "relayJoinCode";
	public static event Action<Lobby, bool> OnLobbyChanged;
	public static bool previouslyRefusedUsername = false;
	public float nextHostHeartbeatTime;
	private const float hostHeartbeatFrequency = 15F;
	public bool wasGameStarted = false;
	
	public ILobbyEvents activeLobbyEvents;

	public PlayerDictionaryData playerDictionaryData { get; private set; }


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
			{ RequestType.UpdateLobbies, 1.35F },
			{ RequestType.DeleteLobby, 2.1F },
			{ RequestType.LeaveOrRemovePlayers, 5.1F},
		};

		/// <summary>
		/// Returns the time to wait for lobby request type
		/// </summary>
		public static float Rate(RequestType type) => TypeRates[type];
		
		/// <summary>
		/// Returns the time to wait for lobby request type in equivalent seconds
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int RateMS(RequestType type) => (int)(TypeRates[type] * 1000F);

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


	private new void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(this);
	}

	/// <summary>
	/// Heartbeat the active lobby if we are the host to keep it alive
	/// </summary>
	async void Update()
    {
		try
		{
			if (activeLobby != null && !wasGameStarted)
			{
				if (isHost && Time.realtimeSinceStartup >= nextHostHeartbeatTime)
				{
					await PeriodicHostHeartbeat();
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Heartbeats the active lobby to keep it active
	/// </summary>
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

	/// <summary>
	/// Update the lobby with our Ready Button state
	/// </summary>
	void UpdateLobby(Lobby updatedLobby)
	{
		// Since this is called after an await, ensure that the Lobby wasn't closed while waiting.
		if (activeLobby == null || updatedLobby == null)
		{
			return;
		}

		activeLobby = updatedLobby;
		
		Debug.Log($"LobbyManager :: We clicked Ready Button. Checking if Game is ready'd up");
		bool isGameReady = AllPlayersReady(activeLobby);

		// Trigger event with value (This starts the game if all players are ready)
		OnLobbyChanged?.Invoke(activeLobby, isGameReady);
	}

	/// <summary>
	/// If Lobby is not null, remove ourselves from it
	/// </summary>
	public void LeaveLobbyOnQuit()
	{
		if (activeLobby != null)
		{
			LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, playerId);
		}
	}

	/// <summary>
	/// Called when the Player is no longer part of a Lobby.
	/// Unsubscribe from events and shutdown the network to clear stale data
	/// </summary>
	public async Task OnPlayerNotInLobby()
	{
		await activeLobbyEvents.UnsubscribeAsync();
		OnLobbyChanged -= OnLobbyChanged;

		if (activeLobby != null)
		{
			activeLobby = null;
		}

		// Shutdown network if not in Gameplay Scene
		if (UIManager.GameView != View.Gameplay)
		{
			StartCoroutine(SessionManager.Instance.IEShutdownNetworkClient());
		}
		
		isHost = false;
		this.wasGameStarted = false;
		
		DebugViewer.Instance.CancelCheck();
	}

	/// <summary>
	/// Returns whether all Players in a lobby are ready using cached player data
	/// </summary>
	static bool AllPlayersReady(Lobby lobby)
	{
		// Cancel if only 1 player
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

	/// <summary>
	/// Creates and returns a Unity Lobby given a Lobby Name, player count, host name, privacy and relay allocation code
	/// </summary>
	/// <returns></returns>
	public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string hostName, bool isPrivate, string relayJoinCode)
	{
		try
		{
			Debug.Log($"LobbyManager :: CreateLobby called with relay join code: {relayJoinCode}");
			playerDictionaryData = new(hostName, false, -1);
			isHost = true;
			wasGameStarted = false;

			// Delete any existing lobby we own
			await DeleteAnyActiveLobbyWithNotify();
			
			if (this == null)
			{
				Debug.LogError("LobbyManager :: CreateLobby Null Ref");
			}

			// Create Lobby Options
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

			// Create and cache the new lobby
			activeLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

			Debug.Log($"LobbyManager :: Lobby created. Stored relay join code: {activeLobby.Data[relayJoinCodeKey].Value}");
			
			// Register lobby event callbacks
			LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
			callbacks.LobbyEventConnectionStateChanged += OnConnectionStateChanged;
			callbacks.LobbyChanged += OnLobbyChangedNotif;
			
			try
			{
				activeLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(activeLobby.Id, callbacks);
			}
			catch (LobbyServiceException ex)
			{
				switch (ex.Reason)
				{
					case LobbyExceptionReason.AlreadySubscribedToLobby: 
						Debug.LogWarning($"Already subscribed to lobby[{activeLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); 
						break;
					case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: 
						Debug.LogWarning($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); 
						await LeaveJoinedLobby();
						break;
					case LobbyExceptionReason.LobbyEventServiceConnectionError: 
						Debug.LogWarning($"Failed to connect to lobby events. Exception Message: {ex.Message}");
						await LeaveJoinedLobby();
						break;
					default: throw;
				}
			}

			if (this == null)
			{
				Debug.LogError("LobbyManager :: CreateLobby Null Ref");
			}
			
			// Create a heartbeat for the lobby to keep it active
			// await LobbyService.Instance.SendHeartbeatPingAsync(activeLobby.Id);
			// if (lobbyHeartbeatRoutine != null)
			// {
			// 	StopCoroutine(lobbyHeartbeatRoutine);
			// }
			// lobbyHeartbeatRoutine = StartCoroutine(HeartbeatLobbyCoroutine(activeLobby.Id, 15));

			CacheLocalPlayer();
			players = activeLobby?.Players;
			LogLobbyCreation(activeLobby);
			DebugViewer.Instance.StartCheck(activeLobby.Id);
		}
		catch (Exception e)
		{
			Debug.LogError($"LobbyManager :: CreateLobby :: Failed to Create Lobby - {e.Message}");
		}
		
		return activeLobby;
	}
	
	// IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
	// {
	// 	var delay = new WaitForSecondsRealtime(waitTimeSeconds);
	//
	// 	while (true)
	// 	{
	// 		LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
	// 		yield return delay;
	// 	}
	// }

	public async Task<Lobby> JoinPrivateLobby(string lobbyJoinCode, string playerName)
	{
		try
		{
			await PrepareToJoinLobby(playerName);
			if (this == null) return null;

			var options = new JoinLobbyByCodeOptions();
			options.Player = CreatePlayerData();

			Debug.Log($"Joining lobby with Code {lobbyJoinCode}");
			activeLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyJoinCode, options);
			if (this == null) return null;
			
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
		wasGameStarted = false;
		playerDictionaryData = new(playerName, false, -1);

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

			await OnPlayerNotInLobby();
			LobbyToMainMenuTransition();
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

	public void LogLobbyPlayers()
	{
		if (activeLobby.Players == null)
		{
			Debug.Log("LobbyManager :: Players are null. Returning");
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

	/// <summary>
	/// Attempt to delete any active lobby owned by us as Host
	/// </summary>
	public async Task DeleteAnyActiveLobbyWithNotify()
	{
		try
		{
			if (activeLobby != null && isHost)
			{
				await LobbyService.Instance.DeleteLobbyAsync(activeLobby.Id);
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

		Debug.Log($"LobbyManager :: Lobby '{lobby.Name}' Created. " +
			$"{lobby.Players.Count}/{lobby.MaxPlayers} Players, " +
			$"Visibility: {(lobby.IsPrivate ? "Private" : "Public")}, " +
			$"Access: {(lobby.IsLocked ? "Locked" : "Unlocked")}, " +
			$"Lobby Code: {lobby.LobbyCode}, " +
			// $"Id: {lobby.Id}, " +
			// $"Created at: {lobby.Created}, " +
			// $"HostId: {lobby.HostId}, " +
			// $"EnvironmentId: {lobby.EnvironmentId}, " +
			// $"Upid: {lobby.Upid}, " +
			$"Data: {lobbyDataStr}");

		Instance.LogLobbyPlayers();
	}
	
	public async Task SetReadyState(bool isReady)
	{
		try
		{
			if (activeLobby == null)
			{
				Debug.Log("LobbyManager :: Attempting to toggle ready state when not already in a lobby.");
				return;
			}

			playerDictionaryData.isReady = isReady;

			var lobbyId = activeLobby.Id;

			var options = new UpdatePlayerOptions();
			options.Data = CreatePlayerDictionary();
			localPlayer.Data = options.Data;

			UIManager.LobbyUI.AdjustLocalPlayerSlotReadyState();

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
	/// Swaps to a new Lobby vehicle, matching the index. Updates the Lobby with changes.
	/// </summary>
	/// <param name="index"></param>
	public async Task SwapLobbyVehicle(int index)
	{
		try
		{
			if (playerDictionaryData.lobbyVehicleIndex == index)
			{
				Debug.Log($"LobbyManager :: Player selected the same vehicle, no need to update Network");
				return;
			}
			
			if (activeLobby == null)
			{
				Debug.Log("LobbyManager :: Attempting to swap vehicle when not already in a lobby.");
				return;
			}
			
			// Re-create player dictionary for Lobby Update
			playerDictionaryData.lobbyVehicleIndex = index;
			UpdatePlayerOptions options = new();
			options.Data = CreatePlayerDictionary();
			localPlayer.Data = options.Data;

			// Adjust the local player slot, showing the new vehicle
			UIManager.LobbyUI.AdjustLocalPlayerSlot();

			// Update the Lobby with our new data
			string lobbyId = activeLobby.Id;
			activeLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
			Debug.Log($"LobbyManager :: Updated Lobby Vehicle for Sync");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}


	/// <summary>
	/// Check if players connected already use our name
	/// Since we are already connected to a lobby, our name will always be found. Avoid that by checking for 2 matches instead of 1
	/// </summary>
	private bool IsPlayerNameValid(string ownerName)
	{
		int matches = 0;
		
		for (int i = 0; i < activeLobby.Players.Count; i++)
		{
			if (ownerName == players[i].Data[PlayerDictionaryData.nameKey].Value)
			{
				matches++;

				// If two players are found with same name, fail this check
				if (matches == 2)
				{
					Debug.Log($"Name Check failed!");
					return false;
				}
			}
		}
		Debug.Log($"Name Check passed!");
		return true;
	}
	
	public void OnConnectionStateChanged(LobbyEventConnectionState newState)
	{
		if (isHost)
		{
			Debug.Log($"LobbyManager :: OnConnectionStateChanged :: Lobby Connection State is {newState} (As Host)");
		}
	}

	/// <summary>
	/// Return the Player to the Main menu
	/// </summary>
	private void LobbyToMainMenuTransition()
	{
		UIManager.PopUntil(UIManager.MainMenu);
		UIManager.LobbySetupMenu.ToggleLobbyCreationInteractables(true);
	}
	
	public void OnLobbyChangedNotif(ILobbyChanges changes)
	{
		if (changes.LobbyDeleted)
		{
			Debug.Log($"Lobby Manager :: Lobby Deleted. Host? {isHost}");

			OnPlayerNotInLobby();

			LobbyToMainMenuTransition();
			UIManager.Instance.PushErrorScreen("Host has closed the Lobby!");
		}
		else
		{
			changes.ApplyToLobby(activeLobby);

			if (changes.PlayerData.Changed || changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
			{
				Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: PlayerData Changed? {changes.PlayerData.Changed}, PlayersJoined? {changes.PlayerJoined.Changed}, PlayerLeft? {changes.PlayerLeft.Changed}");

				CacheLocalPlayer();

				if (activeLobby.Players.Exists(player => player.Id == playerId))
				{
					Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Our Player exists. Checking if Game is ready'd up. Also adjusting player slots etc");
					var isGameReady = AllPlayersReady(activeLobby);

					// Trigger event with value (This starts the game if all players are ready)
					OnLobbyChanged?.Invoke(activeLobby, isGameReady);
				}
				else
				{
					Debug.Log("Lobby Manager :: OnLobbyChangedNotif : Player Not in Lobby");
					OnPlayerNotInLobby();
				}
				return;
			}
			if (changes.PlayerJoined.Changed)
			{
				for (int i = 0; i < changes.PlayerJoined.Value.Count; i++)
				{
					Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Player {changes.PlayerJoined.Value[i].Player.Data[PlayerDictionaryData.nameKey].Value} Joined!");
				}
			}
			if (changes.PlayerLeft.Changed)
			{
				for (int i = 0; i < changes.PlayerLeft.Value.Count; i++)
				{
					Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Player {changes.PlayerLeft.Value[i]} Left!");
				}
			}
		}
	}

	/// <summary>
	/// Attempts to join a private lobby using the specified Join Code and player name.
	/// Ensures Player names are unique. If not, requires user to re-enter name.
	/// </summary>
	public async void JoinPrivateLobbyAsClient(string playerJoinCode, string playerName)
	{
		try
		{
			UIManager.PushPanel(UIManager.LoadingIcon.Prepare("Joining Lobby..."));
			await SessionManager.Instance.InitialiseAndAuthenticatePlayer();
			Lobby joinedLobby = await Instance.JoinPrivateLobby(playerJoinCode, playerName);

			if (this == null || joinedLobby == null)
			{
				Debug.Log("Failed to Join Private Lobby");
				// HANDLE WHEN A LOBBY DOESN'T EXIST
				return;
			}

			// Callbacks
			LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
			callbacks.LobbyEventConnectionStateChanged += OnConnectionStateChanged;
			callbacks.LobbyChanged += OnLobbyChangedNotif;
			
			try
			{
				activeLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(activeLobby.Id, callbacks);
			}
			catch (LobbyServiceException ex)
			{
				FilterLobbyError(ex);
				throw;
			}
			
			// Fetch fresh lobby data to get the current relay join code
			// The initial join response may have stale data
			joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
			
			if (this == null )
				return;
			
			Debug.Log($"Fresh lobby data relay code: {joinedLobby.Data[relayJoinCodeKey].Value}");
			
			activeLobby = joinedLobby;
			
			UIManager.PopPanel();

			if (Instance.activeLobby == null)
				return;

			bool nameCheckPassed = Instance.IsPlayerNameValid(playerName);

			if (nameCheckPassed)
			{
				previouslyRefusedUsername = false;
				Instance.LogLobbyPlayers();
				await OpenLobby(joinedLobby);
				
				DebugViewer.Instance.StartCheck(activeLobby.Id);
			}
			else
			{
				previouslyRefusedUsername = true;
				await Instance.LeaveJoinedLobby();
				
				UIManager.PopAndPush(1, UIManager.FadedBackgroundUI, UIManager.TextInputGroup.Prepare(true, true, TextSubmissionContext.PlayerName));
				UIManager.TextInputGroup.TogglePasteButton(false);
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	private void FilterLobbyError(LobbyServiceException ex)
	{
		switch (ex.Reason)
		{
			case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{activeLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.LobbyNotFound: Debug.LogError($"Lobby not found. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.LobbyFull: Debug.LogError($"Lobby full. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.LobbyLocked: Debug.LogError($"Lobby locked. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.IncorrectPassword: Debug.LogError($"Incorrect Password for lobby. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.InvalidJoinCode: Debug.LogError($"Invalid Join Code. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.Gone: Debug.LogError($"Lobby no longer exists. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.RateLimited: Debug.LogError($"You have tried to join a lobby to many times in short period. Please wait 30s and try again. Exception Message: {ex.Message}"); break;
			case LobbyExceptionReason.AlreadyUnsubscribedFromLobby: Debug.LogError($"{ex.Message}"); break;
			case LobbyExceptionReason.Unknown: Debug.LogError($"An unknown error occured. Exception Message: {ex.Message}"); break;
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

			UIManager.PopAndPush(1, UIManager.LobbyUI.Prepare(lobbyJoined.LobbyCode, lobbyJoined.Name));
			CacheLocalPlayer();
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
			{
				localPlayer = activeLobby.Players[i];
				localPlayerIndex = i;
			}
		}

		return;
	}
	
	public void OnGameStarted()
	{
		// When game actually starts, the host stops updating
		if (isHost)
		{
			wasGameStarted = true;
		}

		// When the game actually starts, all clients clear the active lobby. This is possible because the host will
		// actually delete the lobby itself once all clients have acknowledged that they've started.
		else
		{
			activeLobby = null;
		}
	}
}