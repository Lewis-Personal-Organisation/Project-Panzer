using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameplaySceneManager : Singleton<GameplaySceneManager>
{
    [SerializeField] private GameplayNetworkManager gameplayNetworkManagerPrefab;

    public bool didPlayerPressLeaveButton { get; private set; }


    [field: SerializeField] public Transform[] spawnPoints { get; private set; } = new Transform[4];
    [SerializeField] public TextMeshProUGUI timer;
    
    private new void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        base.OnDestroy();
    }
    
    private void Start()
    {
        spawnPoints.Shuffle();
        
        if (NetworkManager.Singleton != null)
        {
            // Subscribe to the callback when a client disconnects
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            
            // You can also listen for when the server stops
            // NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }
        
        if (LobbyManager.Instance.isHost)
        {
            GameplayNetworkManager.Instantiate(gameplayNetworkManagerPrefab);
        }
        
        GameplayUI.Instance.UpdateScores();
    }

    public void SetCountdown(int seconds)
    {
        GameplaySceneManager.Instance.timer.text = $"{seconds}";
    }
    
    public void ShowGameTimer(int seconds)
    {
        // sceneView.arenaUiOverlayPanelView.ShowGameTimer(seconds);
    }
    
    public void OnGameOver(DataStructs.GameResultsData results)
    {
        // Update player stats so they're available for the results Panel.
        // Note that we do not need to wait for async to finish writing as they won't be needed again until the
        // end of the next game anyway.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        CloudSaveManager.Instance.UpdatePlayerStats(results);
#pragma warning restore CS4014

        // Save off game results so they can be shown when we return to the main menu.
        // Note: This simplifies exiting the game since it can be gracefully-destructed right now without having
        // to worry about whether the host or client leaves first.
        UIManager.Instance.SetPreviousGameResults(results);
        // ServerlessMultiplayerGameSampleManager.instance.SetPreviousGameResults(results);
    }
    
    // private void OnServerStopped(bool wasHost)
    // {
    //     // This gets called on all clients when the server stops
    //     if (!NetworkManager.Singleton.IsServer)
    //     {
    //         Debug.Log("Server stopped - host quit!");
    //         HandleHostDisconnect();
    //     }
    // }
    
    // private void HandleHostDisconnect()
    // {
    //     // Your logic here: show UI, return to menu, attempt reconnection, etc.
    //     // Example: Load main menu scene
    //     NetworkManager.Singleton.Shutdown();
    //     
    //     // Subscribe to the callback when a client disconnects
    //     NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    //         
    //     UIManager.PopUntil(UIManager.MainMenu);
    // }

    private void OnApplicationQuit()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Quiting...");
        
        // Properly shutdown network if we are associating with other players
        if (sceneName == SceneHelper.Instance.mainGameplayScene.Name ||
            sceneName == SceneHelper.Instance.mainMenuScene.Name && LobbyManager.Instance.activeLobbyEvents != null)
        {
            Debug.Log($"... Shutting down Net");
            NetworkManager.Singleton.Shutdown();
        }
    }

    private async void OnClientDisconnect(ulong clientId)
    {
        bool ReturnToMenu = false;

        string message = ""; ;
        
        if (NetworkManager.Singleton.IsServer)
        {
            if (clientId != NetworkManager.ServerClientId)
            {
                // When a client disconnects, we should show a popup ingame here!
                // message = $"Other client ({clientId}) has disconnected! Remaining players: {NetworkManager.Singleton.ConnectedClients.Count}";
            }
            else
            {
                message = $"We have Disconnected and closed the game session!";
                ReturnToMenu = true;
            }
        }
        else
        {
            message = "We have disconnected!";
            ReturnToMenu = true;
        }
        
        if (ReturnToMenu)
        {
            await LobbyManager.Instance.activeLobbyEvents.UnsubscribeAsync();
            LobbyManager.Instance.activeLobbyEvents = null;
            StartCoroutine(WaitForSceneLoad(message));
        }
    }
    
    private IEnumerator WaitForSceneLoad(string message)
    {
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(SceneHelper.Instance.mainMenuScene.Name, LoadSceneMode.Single);
        yield return new WaitUntil(() => asyncLoadLevel.isDone);
        UIManager.Instance.PushErrorScreen(message);
    }
}