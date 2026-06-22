using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.Mathematics;
using Unity.Services.Lobbies.Models;

public class GameplayNetworkManager : NetworkSingleton<GameplayNetworkManager>
{
    private const float COUNTDOWN_DURATION = 5F;
    
    private const float CLIENT_TIMEOUT = COUNTDOWN_DURATION + 5;
    public float clientTimeout = float.MaxValue;
    [SerializeField] float m_GameEndsTime = float.MaxValue;
    private bool m_IsCountdownActive = false;
    private float m_GameCountdownEndsTime = float.MaxValue;
    // We count responses for all players to know when all are in game so we can destroy the lobby.
    public int gameplayStartAcknowledgments = 0;
    // Wait for all clients to acknowledge game results before returning to main menu / results Panel
    int m_NumGameOverAcknowledgments = 0;
    // Be sure to stop all processing once local player avatar is removed.
    bool m_IsShuttingDown = false;
    // Only send updated countdown value when it changes.
    int m_PreviousCountdownSeconds;
    int m_LastGameTimerShown;
    private bool playerCountdown;
    private float countdownTimer = 5F;
    
    [SerializeField] RemoteConfigManager.PlayerOptionsConfig m_PlayerOptions;
    
    [SerializeField] private PlayerAvatar[] playerAvatarPrefabs;
    [SerializeField] private CameraController playerCameraPrefab;
    [field: SerializeField] public NetworkObject networkObject { get; private set; }
    
    public static ulong hostRelayClientId => NetworkManager.Singleton.ConnectedClients[0].ClientId;     // The host is the first connected client

    public Lobby cachedLobby = null;
    public string GetPlayerID(int playerIndex) => Instance.cachedLobby.Players[playerIndex].Id;
    public string GetPlayerName(int playerIndex) => Instance.cachedLobby.Players[playerIndex].Data[LobbyManager.PlayerDictionaryData.nameKey].Value;
    public string GetPlayerVehicleIndex(int playerIndex) => Instance.cachedLobby.Players[playerIndex].Data[LobbyManager.PlayerDictionaryData.vehicleIndexKey].Value;
    
    public List<PlayerAvatar> playerAvatars { get; private set; } = new List<PlayerAvatar>();
    private PlayerAvatar localPlayerAvatar;
    public string localPlayerName => localPlayerAvatar.playerName;
    
    
    private new void Awake()
    {
        base.Awake();
    }
    
    // When the GameManager is instantiated, the game is ready to begin. This kicks off the game by
    // intantiating player avatars and starting the countdown (host only).
    void Start()
    {
        cachedLobby = LobbyManager.Instance.activeLobby;
        LobbyDebugViewer.Instance.CancelCheck();
        Extensions.Debug.ClearConsole();
        
        if (NetworkManager.Singleton != null)
        {
            // Subscribe to the callback when a client disconnects
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;

            // You can also listen for when the server stops
            // NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }
        
        LogLobbyPlayers();
        
        if (IsHost)
        {
            InitializeHostGame();
        }
        else
        {
            InitializeClientGame();
        }
    }
    
    protected override void OnDestroy()
    {
        NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        base.OnDestroy();
    }
    
    void Update()
    {
        // Be sure to stop all processing once local player avatar is removed.
        if (m_IsShuttingDown) return;

        // Check if the Player timed out
        if (Time.realtimeSinceStartup >= clientTimeout && localPlayerAvatar == null)
        {
            Debug.Log("Client timed out so shutting down.");
            Shutdown();

            return;
        }

        if (IsHost)
        {
            UpdateHost();
        }
    }
    
    /// <summary>
    /// The Callback method for processing disconnection events
    /// </summary>
    private async void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData data)
    {
        switch (data.EventType)
        {
            case ConnectionEvent.ClientConnected:
                break;
            case ConnectionEvent.PeerConnected:
                break;
            case ConnectionEvent.ClientDisconnected:
                OnClientDisconnect(data.ClientId);
                break;
            case ConnectionEvent.PeerDisconnected:
                break;
            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }
    
    /// <summary>
    /// The Callback method for when a client disconnects during gameplay
    /// Only received on Server and diconnected client!
    /// </summary>
    public async void OnClientDisconnect(ulong clientId)
    {
        bool ReturnToMenu = false;
        string message = ""; ;
        
        // If Server
        if (NetworkManager.Singleton.IsServer)
        {
            if (clientId != NetworkManager.ServerClientId)
            {
                // When a client disconnects, we should show a popup ingame here!
                GameplayUI.Notifications.GlobalMessage($"Player {GetPlayerName((int)clientId)} disconnected!");
                message = $"Client {GetPlayerName((int)clientId)} ({clientId}) disconnected! Remaining players: {NetworkManager.Singleton.ConnectedClients.Count}";
            }
            else
            {
                message = $"We have Disconnected and closed the gameplay session!";
                ReturnToMenu = true;
            }
        }
        // If Client only (the disconnected client)
        else
        {
            message = "We have disconnected from the gameplay session!";
            ReturnToMenu = true;
        }
        
        Debug.Log($"GameplaySceneManager :: OnClientDisconnect :: Message  -> {message}");
        if (ReturnToMenu)
        {
            await LobbyManager.Instance.activeLobbyEvents.UnsubscribeAsync();
            StartCoroutine(ResetToMainMenu(message));
        }
    }
    
    /// <summary>
    /// Loads the Main Menu scene and returns the player to it
    /// Also shuts down the Network Manager
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private IEnumerator ResetToMainMenu(string message)
    {
        PersistentDataHost.errorMessage = message;
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(SceneHelper.Instance.mainMenuScene.Name, LoadSceneMode.Single);
        yield return new WaitUntil(() => asyncLoadLevel.isDone);
        yield return StartCoroutine(SessionManager.Instance.IEShutdownNetworkClient());
    }
    
    /// <summary>
    /// Spawns this gameobject as an instance, with host ownership
    /// </summary>
    /// <param name="gameplayManagerPrefab"></param>
    static public void Instantiate(GameplayNetworkManager gameplayManagerPrefab)
    {
        var gameManager = GameObject.Instantiate(gameplayManagerPrefab);
        gameManager.networkObject.SpawnWithOwnership(hostRelayClientId);
    }
    
    /// <summary>
    /// Logs all players in the cached lobby
    /// </summary>
    public void LogLobbyPlayers()
    {
        if (cachedLobby.Players == null)
        {
            Debug.Log("GameplayNetworkManager :: LogLobbyPlayers :: Players are null. Returning");
            return;
        }

        string lobbyPlayerNames = "Player(s) ";
        for (int i = 0; i < cachedLobby.Players.Count; i++)
        {
            lobbyPlayerNames += $"'{cachedLobby.Players[i].Data[LobbyManager.PlayerDictionaryData.nameKey].Value}'";

            if (i == cachedLobby.Players.Count - 1)
                lobbyPlayerNames += " are present";
            else
                lobbyPlayerNames += ", ";
        }

        Debug.Log($"GameplayNetworkManager :: LogLobbyPlayers :: {lobbyPlayerNames}");
    }
    
    /// <summary>
    /// Returns the Player to the Main menu
    /// </summary>
    void Shutdown()
    {
        Debug.LogWarning($"Local Player took to long to load or didn't timed out. Returning to main menu!");

        // Be sure to stop all processing once local player avatar is removed.
        m_IsShuttingDown = true;

        // TODO: TEST. ID might be incorrect
        OnClientDisconnect(localPlayerAvatar.OwnerClientId);
    }
    
    /// <summary>
    /// Counts down the start of round timer, if all players are connected
    /// </summary>
    void UpdateHost()
    {
        if (m_IsCountdownActive)
        {
            HostUpdateCountdown();
        }
        // else
        // {
            // if (Time.time >= m_GameEndsTime)
            // {
                // GameEndManager.Instance?.HostGameOver();
            // }
            // else
            // {
                // HostUpdateGameTime();
            // }
        // }
    }
    
    // void HostUpdateGameTime()
    // {
    //     var timeRemaining = Mathf.CeilToInt(m_GameEndsTime - Time.time);
    //     if (timeRemaining != m_LastGameTimerShown)
    //     {
    //         m_LastGameTimerShown = timeRemaining;
    //
    //         UpdateGameTimerClientRpc(timeRemaining);
    //     }
    // }
    
    // [ClientRpc]
    // void UpdateGameTimerClientRpc(int seconds)
    // {
    //     GameplaySceneManager.Instance?.ShowGameTimer(seconds);
    // }
    
    /// <summary>
    /// Updates the countdown as host. Network updates are only sent every 1 second subtracted from the timer
    /// </summary>
    void HostUpdateCountdown()
    {
        countdownTimer -= Time.deltaTime;
        int countdownInSeconds = (int)Mathf.Ceil(countdownTimer);
        
        if (countdownInSeconds != m_PreviousCountdownSeconds)
        {
            m_PreviousCountdownSeconds = countdownInSeconds;

            UpdateCountdownClientRpc(countdownInSeconds);

            if (countdownInSeconds <= 0)
            {
                m_IsCountdownActive = false;
                GameplayUI.CountdownGroup.ToggleWaitAnimation(false);
            }
        }
    }
    
    [ClientRpc]
    void UpdateCountdownClientRpc(int seconds)
    {
        GameplaySceneManager.Instance?.SetCountdown(seconds);

        if (seconds <= 0)
        {
            StartPlayingGame();
        }

        // Refresh player names and starting scores each second to ensure the list is populated.
        OnScoreChanged();
    }
    
    void StartPlayingGame()
    {
        m_GameEndsTime = Time.time + m_PlayerOptions.gameDuration;

        if (localPlayerAvatar != null)
        {
            Debug.Log($"GameplayNetworkManager :: StartPlayingGame() :: Timer complete, enabling player control");
            localPlayerAvatar.vehicleController.inputManager.enabled = true;
            GameplayUI.CountdownGroup.ToggleCountdownTimer(false);
        }
    }
    
    void OnScoreChanged()
    {
        GameplayUI.Instance.UpdateScores();
    }
    
    public void InitializeHostGame()
    {
        Debug.Log("Host starting game...");

        SpawnAllPlayers();

        InitializeGame();
    }

    public void InitializeClientGame()
    {
        Debug.Log("Client starting game...");

        InitializeGame();
    }
    
    void SpawnAllPlayers()
    {
        IReadOnlyDictionary<ulong, NetworkClient> connectedClients = NetworkManager.Singleton.ConnectedClients;
        // var numPlayers = connectedClients.Count;
        // Debug.Log($"Gameplay Network Manager :: SpawnAllPlayers :: Player Count = {numPlayers}");
        
        var playerIndex = 0;
        
        foreach (ulong relayClientId in connectedClients.Keys)
        {
            // int vehicleIndex = int.Parse(cachedLobby.Players[playerIndex].Data[LobbyManager.PlayerDictionaryData.vehicleIndexKey].Value);
            // Debug.Log($"Player {playerIndex} should spawn with vehicle {VehicleData.GetLobbyItem(vehicleIndex).name} using {vehicleIndex}");
            
            SpawnPlayer(playerIndex, relayClientId);
            playerIndex++;
        }
    }
    
    void SpawnPlayer(int playerIndex, ulong relayClientId)
    {
        Vector3 pos = GameplaySceneManager.Instance.spawnPoints[playerIndex].position;
        quaternion rot = GameplaySceneManager.Instance.spawnPoints[playerIndex].rotation;
        
        PlayerAvatar playerAvatar = GameObject.Instantiate(playerAvatarPrefabs[playerIndex], pos, rot);
        playerAvatar.gameObject.name = playerAvatarPrefabs[playerIndex].name;           // Remove clone from name field
        playerAvatar.networkObject.SpawnWithOwnership(relayClientId);
        playerAvatar.SetPlayerAvatarClientRpc(playerIndex, GetPlayerID(playerIndex), GetPlayerName(playerIndex), relayClientId);
        Debug.Log($"GameplayNetworkManager :: SpawnPlayer :: Spawned Player with ID {playerIndex}", playerAvatar.gameObject);
    }
    
    public void AddPlayerAvatar(PlayerAvatar playerAvatar, bool isLocalPlayer)
    {
        playerAvatars.Add(playerAvatar);
    }

    public void SetLocalAvatar(PlayerAvatar playerAvatar)
    {
        localPlayerAvatar = playerAvatar;
        localPlayerAvatar.gameObject.name += " (Local Player)";
    }
    
    void InitializeGame()
    {
        // Set a timeout if we have not yet setup the avatar. If we have, we'll watch to ensure it isn't
        // destroyed by host. Either way, we return to the lobby if the avatar is missing after timeout.
        clientTimeout = localPlayerAvatar == null ? Time.realtimeSinceStartup + CLIENT_TIMEOUT : 0;

        m_GameCountdownEndsTime = Time.time + COUNTDOWN_DURATION;
        
        LobbyManager.Instance.OnGameStarted();
        
        m_PlayerOptions = RemoteConfigManager.Instance.GetConfigForPlayers(cachedLobby.Players.Count);

        // Inform host that this player has started the game. Once all players have started (and thus
        // stopped using the lobby they joined with) the lobby will be deleted by the host.
        PlayerStartedGameServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    void PlayerStartedGameServerRpc()
    {
        gameplayStartAcknowledgments++;
        
        if (gameplayStartAcknowledgments >= playerAvatars.Count)
        {
            // Delete and clear active lobby on this host (i.e. server). Note that we do not await since we are entering starting
            // the game now and do not need to act on deletion or confirm that it's successfully deleted. If it fails for any
            // reason (which it shouldn't) the lobby will simply time out and disappear anyway.
#pragma warning disable CS4014  // Because this call is not awaited, execution of the current method continues before the call is completed
            LobbyManager.Instance.DeleteAnyActiveLobbyWithNotify();
#pragma warning restore CS4014
            m_IsCountdownActive = true;
            GameplayUI.CountdownGroup.ToggleWaitAnimation(true);
        }
    }
    
    public void OnGameOver(string gameResultsJson)
    {
        GameOverClientRpc(gameResultsJson);
    }

    [ClientRpc]
    void GameOverClientRpc(string gameResultsJson)
    {
        m_IsShuttingDown = true;

        GameplaySceneManager.Instance?.ShowGameTimer(0);

        // By using the results passed from host, we ensure all players show the same results and allow
        // the host to pick a random winner if players tie.
        var results = JsonUtility.FromJson<DataStructs.GameResultsData>(gameResultsJson);
        GameplaySceneManager.Instance?.OnGameOver(results);

        GameOverAcknowledgedServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    void GameOverAcknowledgedServerRpc()
    {
        m_NumGameOverAcknowledgments++;
        if (m_NumGameOverAcknowledgments >= playerAvatars.Count)
        {
            Debug.Log("Gameplay Network Manager :: GameOverAcknowledgedServerRpc :: Game Over - Despawning");
            networkObject.Despawn(true);

            // Load the main menu. Note that this will cause the host and all clients to change scenes
            // which will automatically cause this GameManager to be destroyed (including all mirrored
            // Network Objects on all clients).
            NetworkManager.Singleton.SceneManager.LoadScene("ServerlessMultiplayerGameSample", LoadSceneMode.Single);
        }
    }
}
