using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

[DisallowMultipleComponent]
public class CloudSaveManager : Singleton<CloudSaveManager>
{
    const string k_PlayerStatsKey = "MULTIPLAYER_GAME_PLAYER_STATS";

    const int k_MaxHighScores = 3;

    public static CloudSaveManager instance { get; private set; }

    public DataStructs.PlayerStats playerStats => m_PlayerStats;
    [SerializeField] DataStructs.PlayerStats m_PlayerStats;

    public string playerName => playerStats.playerName;

    private new void Awake()
    {
        base.Awake();
    }

    public async Task LoadAndCacheData()
    {
        try
        {
            var savedData = await CloudSaveService.Instance.Data.LoadAllAsync();

            // Check that scene has not been unloaded while processing async wait to prevent throw.
            if (this == null) return;

            if (savedData.TryGetValue(k_PlayerStatsKey, out var playerStatsJson))
            {
                m_PlayerStats = JsonUtility.FromJson<DataStructs.PlayerStats>(playerStatsJson);
            }
            else
            {
                m_PlayerStats = default;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task SetPlayerName(string playerName)
    {
        try
        {
            m_PlayerStats.playerName = playerName;
            Debug.Log($"SetPlayerName: {m_PlayerStats.playerName}");

            var data = new Dictionary<string, object>();
            data[k_PlayerStatsKey] = JsonUtility.ToJson(m_PlayerStats);

            await SaveUpdatedData(data);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task UpdatePlayerStats(DataStructs.GameResultsData results)
    {
        var playerId = AuthenticationService.Instance.PlayerId;

        UpdateWinCount(playerId, results);

        var currentGameScore = GetPlayerScore(playerId, results);

        UpdateHighScores(currentGameScore);

        m_PlayerStats.gameCount++;

        await SavePlayerStats();
    }

    void UpdateWinCount(string playerId, DataStructs.GameResultsData results)
    {
        var didWin = results.winnerPlayerId == playerId;
        if (didWin)
        {
            m_PlayerStats.winCount++;
        }
    }

    int GetPlayerScore(string playerId, DataStructs.GameResultsData results)
    {
        foreach (var playerScore in results.playerScoreData)
        {
            if (playerScore.playerId == playerId)
            {
                return playerScore.score;
            }
        }

        return 0;
    }

    void UpdateHighScores(int currentGameScore)
    {
        if (m_PlayerStats.highScores is null)
        {
            m_PlayerStats.highScores = new List<int>();
        }

        m_PlayerStats.highScores.Add(currentGameScore);
        m_PlayerStats.highScores.Sort();
        m_PlayerStats.highScores.Reverse();
        while (m_PlayerStats.highScores.Count > k_MaxHighScores)
        {
            m_PlayerStats.highScores.RemoveAt(k_MaxHighScores);
        }
    }

    async Task SavePlayerStats()
    {
        try
        {
            var data = new Dictionary<string, object>();
            data[k_PlayerStatsKey] = JsonUtility.ToJson(m_PlayerStats);

            Debug.Log($"Saving updated player stats: {data[k_PlayerStatsKey]}");

            await SaveUpdatedData(data);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    static async Task SaveUpdatedData(Dictionary<string, object> data)
    {
        try
        {
            await CloudSaveService.Instance.Data.ForceSaveAsync(data);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
