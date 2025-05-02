using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplaySceneManager : Singleton<GameplaySceneManager>
{
    [SerializeField] private GameplayNetworkManager gameplayNetworkManagerPrefgab;

    
    private new void Awake()
    {
        base.Awake();
    }
    
    public void SetCountdown(int seconds)
    {
        // sceneView.arenaUiOverlayPanelView.ShowCountdown();
        // sceneView.arenaUiOverlayPanelView.SetCountdown(seconds);
    }
    
    public void UpdateScores()
    {
        // sceneView.UpdateScores();
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
        CloudSaveManager.instance.UpdatePlayerStats(results);
#pragma warning restore CS4014

        // Save off game results so they can be shown when we return to the main menu.
        // Note: This simplifies exiting the game since it can be gracefully-destructed right now without having
        // to worry about whether the host or client leaves first.
        UIManager.Instance.SetPreviousGameResults(results);
        // ServerlessMultiplayerGameSampleManager.instance.SetPreviousGameResults(results);
    }
}
