using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameplayNetworkManager : NetworkSingleton<GameplayNetworkManager>
{
    private const float COUNTDOWN_DURATION = 5F;
    
    private const float CLIENT_TIMEOUT = COUNTDOWN_DURATION + 3;
    float clientTimeout = float.MaxValue;
    float m_GameEndsTime = float.MaxValue;
    private bool m_IsCountdownActive = false;
    private float m_GameCountdownEndsTime = float.MaxValue;
    // We count responses for all players to know when all are in game so we can destroy the lobby.
    int m_NumStartGameAcknowledgments = 0;
    // Wait for all clients to acknowledge game results before returning to main menu / results Panel
    int m_NumGameOverAcknowledgments = 0;
    // Be sure to stop all processing once local player avatar is removed.
    bool m_IsShuttingDown = false;
    // Only send updated countdown value when it changes.
    int m_PreviousCountdownSeconds;
    int m_LastGameTimerShown;
    RemoteConfigManager.PlayerOptionsConfig m_PlayerOptions;
    
    [SerializeField] float playerMinRadius = 1.5f;
    [SerializeField] float playerRadiusIncreasePerPlayer = 1;
    [SerializeField] PlayerAvatar[] playerAvatarPrefabs;
    public NetworkObject networkObject { get; private set; }
    
    // The host is always the first connected client in the Network Manager.
    public static ulong hostRelayClientId => NetworkManager.Singleton.ConnectedClients[0].ClientId;
    
    public List<PlayerAvatar> playerAvatars { get; private set; } = new List<PlayerAvatar>();
    private PlayerAvatar localPlayerAvatar;
    
    
    new private void Awake()
    {
        base.Awake();
    }
    
    // When the GameManager is instantiated, the game is ready to begin. This kicks off the game by
    // intantiating player avatars and starting the countdown (host only).
    void Start()
    {
        if (IsHost)
        {
            InitializeHostGame();
        }
        else
        {
            InitializeClientGame();
        }
    }
    
    void Update()
    {
        // Be sure to stop all processing once local player avatar is removed.
        if (m_IsShuttingDown) return;

        if (DidClientTimeout())
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
    
    bool DidClientTimeout()
    {
        if (Time.realtimeSinceStartup >= clientTimeout)
        {
            if (localPlayerAvatar == null)
            {
                return true;
            }
        }

        return false;
    }
    
    void Shutdown()
    {
        Debug.Log($"Local player's avatar disappeared or didn't appear so returning to lobby.");

        // Be sure to stop all processing once local player avatar is removed.
        m_IsShuttingDown = true;

        GameEndManager.Instance?.ReturnToMainMenu();
    }
    
    void UpdateHost()
    {
        if (m_IsCountdownActive)
        {
            HostUpdateCountdown();
        }
        else
        {
            if (Time.time >= m_GameEndsTime)
            {
                GameEndManager.Instance?.HostGameOver();
            }
            else
            {
                // GameCoinManager.instance?.HostHandleSpawningCoins();

                HostUpdateGameTime();
            }
        }
    }
    
    void HostUpdateGameTime()
    {
        var timeRemaining = Mathf.CeilToInt(m_GameEndsTime - Time.time);
        if (timeRemaining != m_LastGameTimerShown)
        {
            m_LastGameTimerShown = timeRemaining;

            UpdateGameTimerClientRpc(timeRemaining);
        }
    }
    
    [ClientRpc]
    void UpdateGameTimerClientRpc(int seconds)
    {
        GameplaySceneManager.Instance?.ShowGameTimer(seconds);
    }
    
    void HostUpdateCountdown()
    {
        var countdownSeconds = (int)Mathf.Ceil(m_GameCountdownEndsTime - Time.time);

        if (countdownSeconds != m_PreviousCountdownSeconds)
        {
            m_PreviousCountdownSeconds = countdownSeconds;

            UpdateCountdownClientRpc(countdownSeconds);

            if (countdownSeconds <= 0)
            {
                m_IsCountdownActive = false;
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

        // GameCoinManager.instance?.Initialize(IsHost, m_PlayerOptions);

        // GameCoinManager.instance?.StartTimerToSpawnCoins();

        // GameSceneManager.instance?.HideCountdown();

        if (localPlayerAvatar != null)
        {
            localPlayerAvatar.AllowMovement();
        }
    }
    
    void OnScoreChanged()
    {
        GameplaySceneManager.Instance?.UpdateScores();
    }
    

    static public void Instantiate(GameplayNetworkManager gameplayManagerPrefab)
    {
        GameObject.Instantiate(gameplayManagerPrefab).networkObject.SpawnWithOwnership(hostRelayClientId);
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
        var numPlayers = connectedClients.Count;

        var angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
        var spacing = Mathf.PI * 2 / numPlayers;
        var radius = playerMinRadius + playerRadiusIncreasePerPlayer * numPlayers;

        var playerIndex = 0;
        foreach (var relayClientId in connectedClients.Keys)
        {
            var position = new Vector3(Mathf.Cos(angle) * radius, 0,
                Mathf.Sin(angle) * radius);

            SpawnPlayer(playerIndex, relayClientId, position);

            angle += spacing;

            playerIndex++;
        }
    }
    
    void SpawnPlayer(int playerIndex, ulong relayClientId, Vector3 position)
    {
        var playerAvatarPrefab = playerAvatarPrefabs[playerIndex];
        var playerAvatar = GameObject.Instantiate(playerAvatarPrefab, position, Quaternion.identity);

        playerAvatar.networkObject.SpawnWithOwnership(relayClientId);

        var playerId = LobbyManager.Instance.GetPlayerID(playerIndex);
        var playerName = LobbyManager.Instance.GetPlayerName(playerIndex);
        playerAvatar.SetPlayerAvatarClientRpc(playerIndex, playerId, playerName, relayClientId);
    }
    
    public void AddPlayerAvatar(PlayerAvatar playerAvatar, bool isLocalPlayer)
    {
        playerAvatars.Add(playerAvatar);

        if (isLocalPlayer)
        {
            localPlayerAvatar = playerAvatar;
        }
    }
    
    void InitializeGame()
    {
        // Set a timeout if we have not yet setup the avatar. If we have, we'll watch to ensure it isn't
        // destroyed by host. Either way, we return to the lobby if the avatar is missing after timeout.
        clientTimeout = localPlayerAvatar == null ? Time.realtimeSinceStartup + CLIENT_TIMEOUT : 0;

        m_IsCountdownActive = true;
        m_GameCountdownEndsTime = Time.time + COUNTDOWN_DURATION;

        var numPlayers = LobbyManager.Instance.numPlayers;
        m_PlayerOptions = RemoteConfigManager.Instance.GetConfigForPlayers(numPlayers);

        LobbyManager.Instance.OnGameStarted();

        // Inform host that this player has started the game. Once all players have started (and thus
        // stopped using the lobby they joined with) the lobby will be deleted by the host.
        PlayerStartedGameServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    void PlayerStartedGameServerRpc()
    {
        m_NumStartGameAcknowledgments++;
        if (m_NumStartGameAcknowledgments >= playerAvatars.Count)
        {
            // Delete and clear active lobby on this host (i.e. server). Note that we do not await since we are entering starting
            // the game now and do not need to act on deletion or confirm that it's successfully deleted. If it fails for any
            // reason (which it shouldn't) the lobby will simply time out and disappear anyway.
#pragma warning disable CS4014  // Because this call is not awaited, execution of the current method continues before the call is completed
            LobbyManager.Instance.DeleteAnyActiveLobbyWithNotify();
#pragma warning restore CS4014
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
            networkObject.Despawn(true);

            // Load the main menu. Note that this will cause the host and all clients to change scenes
            // which will automatically cause this GameManager to be destroyed (including all mirrored
            // Network Objects on all clients).
            NetworkManager.Singleton.SceneManager.LoadScene("ServerlessMultiplayerGameSample", LoadSceneMode.Single);
        }
    }
}
