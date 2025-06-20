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
    
    private new void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    public async void Setup()
    {
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

    void GetConfigValues()
    {
        var configJson = RemoteConfigService.Instance.appConfig.GetJson("MULTIPLAYER_GAME_SETTINGS");
        Debug.Log(configJson);
        settings = JsonUtility.FromJson<SettingsConfig>(configJson);
        vSettings = settings;
        
        Debug.Log($"Read Remote Config settings: {settings}");
    }

    // void OnDestroy()
    // {
    //     Instance?.OnDestroy();
    // }

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
