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
	public static string localPlayerId => AuthenticationService.Instance.PlayerId;
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


	/// <summary>
	/// The Rate Limits class to comply with the Unity UGS rate limit for various services
	/// </summary>
	public static class RateLimits
	{
		public enum RateType
		{
			UpdatePlayers,
			UpdateLobbies,
			DeleteLobby,
			LeaveOrRemovePlayers,
		}

		private static Dictionary<RateType, float> TypeRates = new Dictionary<RateType, float>
		{
			{ RateType.UpdatePlayers, 1.1F },
			{ RateType.UpdateLobbies, 1.35F },
			{ RateType.DeleteLobby, 2.1F },
			{ RateType.LeaveOrRemovePlayers, 5.1F},
		};

		/// <summary>
		/// Returns the time to wait for lobby request type
		/// </summary>
		public static float Rate(RateType type) => TypeRates[type];
		
		/// <summary>
		/// Returns the time to wait for lobby request type in equivalent seconds
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int RateMS(RateType type) => (int)(TypeRates[type] * 1000F);

		/// <summary>
		/// Use to override the default limit of a Request Type
		/// </summary>
		/// <param name="overrideType"></param>
		/// <param name="newLimit"></param>
		public static void OverrideRate(RateType overrideType, float newLimit) => TypeRates[overrideType] = newLimit;
	}

	/// <summary>
	/// The Player Data Dictionary used by Unity Lobby for Player Data tracking
	/// </summary>
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
		
		// If this instance was destroyed by the base class, don't continue
		// if (Instance != this)
		// 	return;
		
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

	void DEBUG_UpdateLobby(Lobby updatedLobby)
	{
		// Since this is called after an await, ensure that the Lobby wasn't closed while waiting.
		if (activeLobby == null || updatedLobby == null)
		{
			return;
		}

		activeLobby = updatedLobby;
		
		Debug.Log($"LobbyManager :: We clicked Ready Button. Checking if Game is ready'd up");
		// bool isGameReady = AllPlayersReady(activeLobby);

		// Trigger event with value (This starts the game if all players are ready)
		OnLobbyChanged?.Invoke(activeLobby, true);
	}

	/// <summary>
	/// If Lobby is not null, remove ourselves from it
	/// </summary>
	public void LeaveLobbyOnQuit()
	{
		if (activeLobby != null)
		{
			LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, localPlayerId);
		}
	}

	/// <summary>
	/// Called when the Player is no longer part of a Lobby.
	/// Unsubscribes from events and shuts down the network to clear stale data
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
		
		LobbyDebugViewer.Instance.CancelCheck();
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
			// Debug.Log($"LobbyManager :: CreateLobby called with relay join code: {relayJoinCode}");
			playerDictionaryData = new(hostName, false, -1);
			isHost = true;
			wasGameStarted = false;

			// Delete any existing lobby we own
			await DeleteAnyActiveLobbyWithNotify();
			
			if (this == null)
			{
				Debug.LogError($"LobbyManager :: CreateLobby :: Task Failed!");
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

			// Debug.Log($"LobbyManager :: Lobby created. Stored relay join code: {activeLobby.Data[relayJoinCodeKey].Value}");
			
			// Register lobby event callbacks
			LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
			callbacks.LobbyEventConnectionStateChanged += OnLobbyConnectionStateChanged;
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
				Debug.LogError($"LobbyManager :: CreateLobby :: Task Failed!");
			}

			CacheLocalPlayer();
			players = activeLobby?.Players;
			LogLobbyCreation(activeLobby);
			LobbyDebugViewer.Instance.StartCheck(activeLobby.Id, activeLobby.IsPrivate);
		}
		catch (Exception e)
		{
			Debug.LogError($"LobbyManager :: CreateLobby :: Failed to Create Lobby - {e.Message}");
		}
		
		return activeLobby;
	}
	
	/// <summary>
	/// Creates the Player Data Object needed to join a lobby and attempts to join a Lobby using provided lobby code and name
	/// Caches the players in the lobby
	/// </summary>
	/// <param name="lobbyJoinCode"></param>
	/// <param name="playerName"></param>
	/// <returns></returns>
	public async Task<Lobby> JoinPrivateLobby(string lobbyJoinCode, string playerName)
	{
		try
		{
			await PrepareToJoinLobby(playerName);
			
			if (this == null)
			{
				Debug.LogError($"LobbyManager :: JoinPrivateLobby :: Task Failed!");
			}

			var options = new JoinLobbyByCodeOptions();
			options.Player = CreatePlayerData();

			Debug.Log($"Joining lobby with Code {lobbyJoinCode}");
			activeLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyJoinCode, options);
			
			if (this == null)
			{
				Debug.LogError($"LobbyManager :: JoinPrivateLobby :: Task Failed!");
			}
			
			players = activeLobby?.Players;
		}
		catch (Exception e)
		{
			Debug.LogWarning(e);
			if (this == null) return null;

			activeLobby = null;

			LobbyToMainMenuTransition();
			UIManager.PushErrorScreen("Could not join lobby");
		}

		return activeLobby;
	}

	/// <summary>
	/// Prepares the Player for Lobby joining. Leaves any pre-existing connection to a lobby.
	/// </summary>
	/// <param name="playerName"></param>
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

	/// <summary>
	/// Attempts to remove a Player from the lobby. Used in the context of the local player
	/// </summary>
	public async Task LeaveJoinedLobby()
	{
		try
		{
			await RemovePlayerFromLobby(localPlayerId);
			
			if (this == null)
			{
				Debug.LogError($"LobbyManager :: LeaveJoinedLobby :: Task Failed.");
			}

			await OnPlayerNotInLobby();
			LobbyToMainMenuTransition();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Remove a Player from a Unity Lobby with a given ID
	/// </summary>
	/// <param name="playerId"></param>
	public async Task RemovePlayerFromLobby(string playerId)
	{
		try
		{
			if (activeLobby != null)
			{
				await LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, playerId);
				Debug.Log($"LobbyManager :: RemovePlayerFromLobby :: Removed Player with ID {playerId}");
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Logs all players in the active lobby
	/// </summary>
	public void LogLobbyPlayers()
	{
		if (activeLobby.Players == null)
		{
			Debug.Log("LobbyManager :: LogLobbyPlayers :: Players are null. Returning");
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

		Debug.Log($"LobbyManager :: LogLobbyPlayers :: {lobbyPlayerNames}");
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

	/// <summary>
	/// Creates and returns new Player data required for creating or joining a lobby
	/// </summary>
	/// <returns></returns>
	Player CreatePlayerData()
	{
		var player = new Player();
		player.Data = CreatePlayerDictionary();
		return player;
	}

	/// <summary>
	/// Creates and returns a Player Data Dictionary using current player game data
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Logs the active lobby data to the console
	/// </summary>
	/// <param name="lobby"></param>
	public static void LogLobbyCreation(Lobby lobby)
	{
		if (lobby is null)
		{
			Debug.Log($"LobbyManager :: LogLobbyCreation :: No Active Lobby");
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
	
	/// <summary>
	/// Sets the ready state of our Player in the Lobby and synchronises this to other players
	/// </summary>
	/// <param name="isReady"></param>
	public async Task SetReadyState(bool isReady)
	{
		try
		{
			if (activeLobby == null)
			{
				Debug.Log("LobbyManager :: SetReadyState :: Attempting to toggle ready state when not already in a lobby.");
				return;
			}

			playerDictionaryData.isReady = isReady;

			var options = new UpdatePlayerOptions();
			options.Data = CreatePlayerDictionary();
			localPlayer.Data = options.Data;

			PreGameplayUI.Lobby.AdjustLocalPlayerSlotReadyState();

			var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(activeLobby.Id, localPlayerId, options);
			if (this == null) return;

			UpdateLobby(updatedLobby);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public async Task DEBUG_SetReadyState(bool isReady)
	{
		try
		{
			if (activeLobby == null)
			{
				Debug.Log("LobbyManager :: SetReadyState :: Attempting to toggle ready state when not already in a lobby.");
				return;
			}

			playerDictionaryData.isReady = isReady;

			var options = new UpdatePlayerOptions();
			options.Data = CreatePlayerDictionary();
			localPlayer.Data = options.Data;

			PreGameplayUI.Lobby.AdjustLocalPlayerSlotReadyState();

			var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(activeLobby.Id, localPlayerId, options);
			if (this == null) return;

			DEBUG_UpdateLobby(updatedLobby);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Swaps the current Lobby vehicle of the local player and synchronises with other players
	/// Does not attempt to synchronise if the same vehicle is selected
	/// </summary>
	/// <param name="index"></param>
	public async Task SwapLobbyVehicle(int index)
	{
		try
		{
			if (playerDictionaryData.lobbyVehicleIndex == index)
			{
				Debug.Log($"LobbyManager :: SwapLobbyVehicle :: Player selected the same vehicle, no need to update Network");
				return;
			}
			
			if (activeLobby == null)
			{
				Debug.Log($"LobbyManager :: SwapLobbyVehicle :: Attempting to swap vehicle when not already in a lobby.");
				return;
			}
			
			// Re-create player dictionary for Lobby Update
			playerDictionaryData.lobbyVehicleIndex = index;
			UpdatePlayerOptions options = new UpdatePlayerOptions
			{
				Data = CreatePlayerDictionary()
			};
			localPlayer.Data = options.Data;

			// Adjust the local player slot, showing the new vehicle
			PreGameplayUI.Lobby.AdjustLocalPlayerSlot();

			// Update the Lobby with our new data
			string lobbyId = activeLobby.Id;
			activeLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, localPlayerId, options);
			Debug.Log($"LobbyManager :: SwapLobbyVehicle :: Updated Player Lobby Vehicle");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}


	/// <summary>
	/// Check if players connected already use our name
	/// Since we are already connected to a lobby at this point, our name will always be found so check for 2 matches instead of 1
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
	
	/// <summary>
	/// Logs the connection state fired by events
	/// </summary>
	/// <param name="newState"></param>
	public void OnLobbyConnectionStateChanged(LobbyEventConnectionState newState)
	{
		if (isHost)
		{
			Debug.Log($"LobbyManager :: OnConnectionStateChanged :: Lobby Connection State is {newState} (Host)");
		}
		else
		{
			Debug.Log($"LobbyManager :: OnConnectionStateChanged :: Lobby Connection State is {newState} (Client)");
		}
	}

	/// <summary>
	/// Return the Player to the Main menu
	/// </summary>
	private void LobbyToMainMenuTransition()
	{
		UIManager.PopAllAndPush(PreGameplayUI.MainMenu);
		PreGameplayUI.LobbySetupMenu.ToggleLobbyCreationInteractables(true);
	}
	
	/// <summary>
	/// The response method for when Lobby changes are detected on the network. Handles the new lobby data
	/// </summary>
	/// <param name="changes"></param>
	public void OnLobbyChangedNotif(ILobbyChanges changes)
	{
		if (changes.LobbyDeleted)
		{
			OnPlayerNotInLobby();

			LobbyToMainMenuTransition();
			UIManager.PushErrorScreen("Host has closed the Lobby!");
		}
		else
		{
			changes.ApplyToLobby(activeLobby);

			if (changes.PlayerData.Changed || changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
			{
				// Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: PlayerData Changed? {changes.PlayerData.Changed}, PlayersJoined? {changes.PlayerJoined.Changed}, PlayerLeft? {changes.PlayerLeft.Changed}");

				CacheLocalPlayer();

				if (activeLobby.Players.Exists(player => player.Id == localPlayerId))
				{
					// Debug.Log($"LobbyManager :: OnLobbyChangedNotif :: Our Player exists. Checking if Game is ready'd up. Also adjusting player slots etc");
					var isGameReady = AllPlayersReady(activeLobby);

					// Trigger event with value (This starts the game if all players are ready)
					OnLobbyChanged?.Invoke(activeLobby, isGameReady);
				}
				else
				{
					// Debug.Log("Lobby Manager :: OnLobbyChangedNotif : Player Not in Lobby");
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
			UIManager.PushPanel(PreGameplayUI.LoadingIcon.Prepare("Joining Lobby..."));
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
			callbacks.LobbyEventConnectionStateChanged += OnLobbyConnectionStateChanged;
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
				
				LobbyDebugViewer.Instance.StartCheck(activeLobby.Id, activeLobby.IsPrivate);
			}
			else
			{
				previouslyRefusedUsername = true;
				await Instance.LeaveJoinedLobby();
				
				UIManager.PopAndPush(1, PreGameplayUI.FadedBackgroundUI, PreGameplayUI.TextInputGroup.Prepare(true, true));
				PreGameplayUI.TextInputGroup.TogglePasteButton(false);
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Filters the Lobby Exception and logs the appropriate message to console
	/// </summary>
	/// <param name="ex"></param>
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
	/// Shows the Lobby UI once a lobby is joined using Relay
	/// </summary>
	async Task OpenLobby(Lobby lobbyJoined)
	{
		try
		{
			await SessionManager.Instance.InitializeRelayClient(lobbyJoined);

			Debug.Log("Lobby Manager :: OpenLobby :: Lobby Data Retrieved. Initializing Relay Client");
			
			if (this == null)
			{
				Debug.Log("Lobby Manager :: OpenLobby :: Null. Returning");
				return;
			}

			UIManager.PopAndPush(1, PreGameplayUI.Lobby.Prepare(lobbyJoined.LobbyCode, lobbyJoined.Name));
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
			if (localPlayerId == activeLobby.Players[i].Id)
			{
				localPlayer = activeLobby.Players[i];
				localPlayerIndex = i;
			}
		}
	}
	
	/// <summary>
	/// Caches whether the gameplay session has started
	/// </summary>
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