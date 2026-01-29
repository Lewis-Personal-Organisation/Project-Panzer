using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class DebugViewer : Singleton<DebugViewer>
{
    [SerializeField] private string lobbyID = string.Empty;           // The target Lobby's ID
    [SerializeField] private Allocation allocation;
    [SerializeField] private string relayCode;
    [SerializeField] private bool targetLobbyActive = false;          // The target Lobby
    [SerializeField] private float checkTime = 0F;
    [SerializeField] private List<string> lobbyCodes = new List<string>();
    [SerializeField] private CancellationTokenSource cts;

    
    private new void Awake()
    {
        base.Awake();
        
        // If this instance was destroyed by the base class, don't continue
        if (Instance != this)
            return;
        
        DontDestroyOnLoad(this);
    }

    private void OnApplicationQuit()
    {
        if (cts != null)
            CancelCheck();
    }

    public void StartCheck(string lobbyID)
    {
        this.lobbyID = lobbyID;
        cts = new CancellationTokenSource();
        LobbyLoopAsync(cts.Token);
    }

    public void SetAllocationID(Allocation allocation)
    {
        this.allocation = allocation;
    }

    public void CancelCheck()
    {
        cts.Cancel();
        cts.Dispose();
        cts = null;

        lobbyID = string.Empty;
        checkTime = 0;
        lobbyCodes.Clear();
        relayCode = string.Empty;
    }
    
    async Task LobbyLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!string.IsNullOrEmpty(lobbyID))
            {
                targetLobbyActive = await IsLobbyStillActive(lobbyID);
                
                if (allocation != null) 
                    relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                // Debug.Log($"New check time: {Time.realtimeSinceStartup}");
                checkTime = Time.realtimeSinceStartup;
            }

            await Task.Delay(
                LobbyManager.RateLimits.RateMS(
                    LobbyManager.RateLimits.RequestType.UpdateLobbies
                ),
                token
            );
        }
    }
    
    public async Task<bool> IsLobbyStillActive(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            
            if (lobby != null)
            {
                if (!lobbyCodes.Contains(lobby.LobbyCode))
                    lobbyCodes.Add(lobby.LobbyCode);
            }
            return lobby != null;
        }
        catch (LobbyServiceException e)
        {
            // Lobby doesn't exist or you don't have access
            Debug.LogError($"Lobby check failed: {e.Message}");
            return false;
        }
    }
    
}
