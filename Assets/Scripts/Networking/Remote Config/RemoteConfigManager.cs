using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using UnityEngine;

[DisallowMultipleComponent]
public class RemoteConfigManager : Singleton<RemoteConfigManager>
{
    public SettingsConfig settings { get; private set; }
    public SettingsConfig vSettings;
    
    public bool isInitialized { get; private set; }
    
    private new void Awake() { }

    /// <summary>
    /// Sets up the games multiplayer configuration using Unity Config Service
    /// </summary>
    public async void Setup()
    {
        base.Awake();
        
        // If this instance was destroyed by the base class, don't continue
        if (Instance != this)
            return;
        
        DontDestroyOnLoad(this);
        
        try
        {
            if (this == null) return;
            await FetchConfigs();
            
            if (this == null) return;
            
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Requests the Remote Config setup from Unity
    /// </summary>
    public async Task FetchConfigs()
    {
        try
        {
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());

            // Check that scene has not been unloaded while processing async wait to prevent throw.
            if (this == null) return;

            GetConfigValues();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public PlayerOptionsConfig GetConfigForPlayers(int numPlayers)
    {
        foreach (var playerOptions in settings.playerOptions)
        {
            if (playerOptions.players == numPlayers)
            {
                return playerOptions;
            }
        }

        throw new KeyNotFoundException(
            $"Specified number of players ({numPlayers}) not found in Remote Config player options.");
    }

    /// <summary>
    /// Sets the local config data from the retrieved config
    /// </summary>
    private void GetConfigValues()
    {
        var configJson = RemoteConfigService.Instance.appConfig.GetJson("MULTIPLAYER_GAME_SETTINGS");
        settings = JsonUtility.FromJson<SettingsConfig>(configJson);
        vSettings = settings;
        Debug.Log($"Downloaded Remote Config settings: {settings}");
    }

    struct UserAttributes
    {
    }

    struct AppAttributes
    {
    }

    [Serializable]
    public struct SettingsConfig
    {
        public List<PlayerOptionsConfig> playerOptions;

        public override string ToString()
        {
            return
                $"playerSettings: {string.Join("; ", playerOptions.Select(setting => setting.ToString()).ToArray())}";
        }
    }

    /// <summary>
    /// The config data
    /// </summary>
    [Serializable]
    public struct PlayerOptionsConfig
    {
        public int players;
        public float gameDuration;
        public float initialSpawnDelay;
        public float spawnInterval;
        public float destroyInterval;
        public int cluster1;
        public int cluster2;
        public int cluster3;

        public override string ToString()
        {
            return $"{players} players: duration{gameDuration}, initialDelay:{initialSpawnDelay}, " +
                   $"spawnInterval:{spawnInterval}, destroyInterval:{destroyInterval}, " +
                   $"clusters:{cluster1}/{cluster2}/{cluster3}";
        }
    }
}
