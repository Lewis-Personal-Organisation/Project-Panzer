using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Linq;
using System.Collections;

[DisallowMultipleComponent]
public class LobbyManager : Singleton<LobbyManager>
{
	public Lobby activeLobby { get; private set; }
	public static string playerId => AuthenticationService.Instance.PlayerId;
	public List<Player> players { get; private set; }
	
	public Player localPlayer { get; private set; }
	public string localPlayerVehicleKey => localPlayer.Data[PlayerDictionaryData.vehicleIndexKey].Value;
	public int localPlayerIndex { get; private set; }
	public int numPlayers => players.Count;
	public bool isHost { get; private set; }
	public const string hostNameKey = "hostName";
	public const string relayJoinCodeKey = "relayJoinCode";
	public static event Action<Lobby, bool> OnLobbyChanged;
	public static bool previouslyRefusedUsername = false;
	float nextHostHeartbeatTime;
	const float hostHeartbeatFrequency = 15;
	float nextUpdatePlayersTime;
	bool wasGameStarted = false;
	
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

	// private async Task PeriodicGetUpdatedLobby()
	// {
	// 	try
	// 	{
	// 		// Set next update time before calling Lobby Service since next update could also trigger an
	// 		// update which could cause throttling issues.
	// 		nextUpdatePlayersTime = Time.realtimeSinceStartup + RateLimits.Rate(RateLimits.RequestType.UpdatePlayers);
	// 		//Debug.Log($"LobbyManager :: Updating Lobby ({Time.realtimeSinceStartup}s). Interval -> {RateLimits.Rate(RateLimits.RequestType.UpdatePlayers)}s");
	// 		var updatedLobby = await LobbyService.Instance.GetLobbyAsync(activeLobby.Id);
	//
	// 		if (this == null)
	// 		{
	// 			return;
	// 		}
	//
	// 		UpdateLobby(updatedLobby);
	// 	}
	//
	// 	// Handle lobby no longer exists (host canceled game and returned to main menu).
	// 	catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
	// 	{
	// 		if (this == null) return;
	//
	// 		// Lobby has closed
	// 		//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
	// 		//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.LobbyClosed);
	// 		Debug.Log("Lobby Not Found or Closed. Returning to Main Menu");
	// 		OnPlayerNotInLobby();
	// 	}
	//
	// 	// Handle player no longer allowed to view lobby (host booted player so player is no longer in the lobby).
	// 	catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.Forbidden)
	// 	{
	// 		if (this == null) return;
	//
	// 		//ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
	// 		//	ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);
	//
	// 		OnPlayerNotInLobby();
	// 	}
	// 	catch (Exception e)
	// 	{
	// 		Debug.LogException(e);
	// 	}
	// }
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

		activeLobby = updatedLobby;
		
		Debug.Log($"LobbyManager :: We clicked Ready Button. Checking if Game is ready'd up");
		bool isGameReady = AllPlayersReady(activeLobby);

		// Trigger event with value (This starts the game if all players are ready)
		OnLobbyChanged?.Invoke(activeLobby, isGameReady);
		
		// if (DidPlayersChange(activeLobby.Players, updatedLobby.Players))
		// {
		// 	//Debug.Log("Update Lobby :: Players Changed - Lobby Updated!");
		// 	activeLobby = updatedLobby;
		// 	players = activeLobby?.Players;
		// 	CacheLocalPlayer();
		//
		// 	// Check our lobby Players for our Player. If we exist, set the game ready state
		// 	if (updatedLobby.Players.Exists(player => player.Id == playerId))
		// 	{
		// 		var isGameReady = AllPlayersReady(updatedLobby);
		//
		// 		// Trigger event with value (This starts the game if all players are ready)
		// 		OnLobbyChanged?.Invoke(updatedLobby, isGameReady);
		// 	}
		// 	else
		// 	{
		// 		Debug.Log("Update Lobby : Player Kicked");
		// 		ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
		// 			ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);
		//
		// 		OnPlayerNotInLobby();
		// 	}
		// }
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

	public void OnPlayerNotInLobby()
	{
		// activeLobbyEvents.UnsubscribeAsync();
		activeLobbyEvents = null;

		if (activeLobby != null)
		{
			activeLobby = null;
		}

		if (UIManager.GameView != View.Gameplay)
		{
			StartCoroutine(ShutdownNetwork());
		}
	}
	
	private IEnumerator ShutdownNetwork()
	{
		Debug.Log("Lobby Manager :: ShutdownNetwork :: Shutting Down");
		yield return StartCoroutine(SessionManager.Instance.IEShutdownNetworkClient());
		Debug.Log($"Lobby Manager :: ShutdownNetwork :: Network Shutdown successfully");
	}

	// static bool DidPlayersChange(List<Player> oldPlayers, List<Player> newPlayers)
	// {
	// 	if (oldPlayers.Count != newPlayers.Count)
	// 	{
	// 		Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Player Count Changed");
	// 		return true;
	// 	}
	//
	// 	for (int i = 0; i < newPlayers.Count; i++)
	// 	{
	// 		if (oldPlayers[i].Id != newPlayers[i].Id ||
	// 			oldPlayers[i].Data[PlayerDictionaryData.isReadyKey].Value != newPlayers[i].Data[PlayerDictionaryData.isReadyKey].Value)
	// 		{
	// 			Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Player ID/Ready State Changed");
	// 			return true;
	// 		}
	//
	// 		if (oldPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value != newPlayers[i].Data[PlayerDictionaryData.vehicleIndexKey].Value)
	// 		{
	// 			Debug.Log("lobby Manager :: DidPlayersChange :: Updating Lobby > Vehicle Index Changed");
	// 			return true;
	// 		}
	// 	}
	//
	// 	return false;
	// }

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

	public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string hostName, bool isPrivate, string relayJoinCode)
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
			callbacks.LobbyEventConnectionStateChanged += OnConnectionStateChanged;
			//callbacks.PlayerJoined += OnPlayersJoinedLobby;
			//callbacks.PlayerLeft += OnPlayersLeftLobby;
			callbacks.LobbyChanged += OnLobbyChangedNotif;
			// callbacks.LobbyDeleted += OnLobbyDeleted;
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

			CacheLocalPlayer();
			players = activeLobby?.Players;
			LogLobbyCreation(activeLobby);
		}
		catch (Exception e)
		{
			Debug.LogError($"LobbyManager :: CreateLobby :: Failed to Create Lobby - {e.Message}");
		}

		return activeLobby;
	}

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

		Debug.Log($"LobbyManager :: Lobby '{lobby.Name}' Created. " +
			$"{lobby.Players.Count}/{lobby.MaxPlayers} Players, " +
			$"Visibility: {(lobby.IsPrivate ? "Private" : "Public")}, " +
			$"Access: {(lobby.IsLocked ? "Locked" : "Unlocked")}, " +
			$"Lobby Code: {lobby.LobbyCode}, " +
			$"Id: {lobby.Id}, " +
			$"Created at: {lobby.Created}, " +
			$"HostId: {lobby.HostId}, " +
			$"EnvironmentId: {lobby.EnvironmentId}, " +
			$"Upid: {lobby.Upid}, " +
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
	/// Asynchronously await for some amount of time
	/// </summary>
	/// <param name="seconds"></param>
	//private async Task WaitForSecondsAsync(float seconds)
	//{
	//	await Task.Delay(TimeSpan.FromSeconds(seconds));
	//}

	/// <summary>
	/// Check if players connected already use our name
	/// Since we are already connected to a lobby, our name will always be found. Avoid that by checking for 2 matches instead of 1
	/// </summary>
	private bool IsPlayerNameValid(string ownerName)
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
	
	public void OnConnectionStateChanged(LobbyEventConnectionState newState)
	{
		if (isHost)
		{
			Debug.Log($"LobbyManager :: OnConnectionStateChanged :: Lobby Connection State is {newState} (As Host)");
		}
	}
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

	// public void OnLobbyDeleted()
	// {
	// 	Debug.Log($"LobbyManager :: Lobby has been Deleted. Returning to main Menu");
	// }

	// private IEnumerator OnLobbyDeletedClient()
	// {
	// 	
	// }

	private void LobbyToMainMenuTransition()
	{
		UIManager.PopAndPush(1, UIManager.MainMenu);
		UIManager.LobbySetupMenu.ToggleLobbyCreationInteractables(true);
	}
	
	public void OnLobbyChangedNotif(ILobbyChanges changes)
	{
		if (changes.LobbyDeleted)
		{
			if (!isHost)
			{
				Debug.Log("Lobby Deleted");
				OnPlayerNotInLobby();
			}
			
			LobbyToMainMenuTransition();
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
	public async void JoinPrivateLobbyAsClient(string joinCode, string playerName)
	{
		try
		{
			UIManager.PushPanel(UIManager.LoadingIcon.Prepare("Joining Lobby..."));
			await SessionManager.Instance.InitialiseAndAuthenticatePlayer();
			Lobby joinedLobby = await Instance.JoinPrivateLobby(joinCode, playerName);

			if (this == null)
				return;
			
			if (joinedLobby == null)
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

			UIManager.PopPanel();
			Debug.Log($"Checking Name {playerName}");
			Instance.LogLobbyPlayers();

			if (Instance.activeLobby == null)
				return;

			bool nameCheckPassed = Instance.IsPlayerNameValid(playerName);

			if (nameCheckPassed)
			{
				Debug.Log("Name Check: Passed");
				previouslyRefusedUsername = false;
				Instance.LogLobbyPlayers();
				await OpenLobby(joinedLobby);
			}
			else
			{
				Debug.Log("Name Check: Failed. Leaving Lobby... Set a unique name and rejoin!");
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