using UnityEngine;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
#if UNITY_EDITOR 
using ParrelSync;
#endif
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using System.Collections;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.Events;

public class SessionManager : Singleton<SessionManager>
{
	[SerializeField] private NetworkManager networkManagerPrefab;
	[SerializeField] internal UnityTransport unityTransport;
	public string uniqueProfileString = string.Empty;
	private bool IsNetworkReady => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
	[Space(10)]
	[SerializeField] public UnityEvent OnAuthenticated;

	
	private new void Awake()
	{
		base.Awake();
		
		// If this instance was destroyed by the base class, don't continue
		if (Instance != this)
			return;

		NetworkManager netManager = Instantiate(networkManagerPrefab);
		netManager.gameObject.name = "Network Manager";
		unityTransport = netManager.GetComponent<UnityTransport>();
		
		NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
		DontDestroyOnLoad(this.gameObject);
	}

	// The callback method for when the Unity Transport system fails
	private void OnTransportFailure()
	{
		Debug.Log("Transport Failure!");
	}

	/// <summary>
	/// Initialises and Authenticates the Player and retrieves the cloud-stored player data
	/// </summary>
	public async Task InitialiseAndAuthenticatePlayer()
	{
		if (IsNetworkReady)
			return;
		
		await SignIntoUnityServices();
		await AuthenticatePlayer();
		
		// Wait until Cloud Save Manager is ready
		while (!CloudSaveManager.Instance)
			await Task.Yield();
		
		await InitialiseCloudSaveServices();
	}

	/// <summary>
	/// Initialises Unity Services. If in Editor, applies the appropriate profile string.
	/// Else, since we can guarantee we are on separate devices, initialise
	/// </summary>
	private async Task SignIntoUnityServices()
	{
		try
		{
#if UNITY_EDITOR
			uniqueProfileString = GetProjectName();
			if (ClonesManager.IsClone())
			{
				uniqueProfileString += "ClonedProject";
			}

			uniqueProfileString = uniqueProfileString.Replace(" ", "");
			
			if (uniqueProfileString.Length > 29)
				uniqueProfileString = uniqueProfileString.Substring(0, 29);

			InitializationOptions unityServicesAuthOptions = new InitializationOptions().SetProfile(uniqueProfileString);
			await UnityServices.InitializeAsync(unityServicesAuthOptions);
#else
			Debug.Log("Session Manager :: Attempting Unity Services Init for Player...");
			await UnityServices.InitializeAsync();
#endif
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			Debug.Log("Session Manager :: Unity Services initialised Successfully");
		}
	}

	/// <summary>
	/// Authenticates the Player by signing-in anonymously.
	/// On Successful sign-in, OnAuthenticated Event is invoked.
	/// </summary>
	private async Task AuthenticatePlayer()
	{
		AuthenticationService.Instance.SignInFailed += (err) =>
		{
			Debug.LogError($"SessionManager :: {err}");
		};
		AuthenticationService.Instance.SignedOut += () =>
		{
			Debug.Log($"SessionManager :: Player signed out.");
		};
		AuthenticationService.Instance.Expired += () =>
		{
			Debug.LogWarning($"SessionManager :: Player session has expired and Unity was unable to sign in automatically. Attempting manual sign-in.");
		};
		AuthenticationService.Instance.SignedIn += () =>
		{
			Debug.Log($"SessionManager :: Unity Relay :: Player Signed in. ID: {AuthenticationService.Instance.PlayerId}");
			OnAuthenticated?.Invoke();
		};

		try
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Initialises the Cloud Services and Loads its data
	/// </summary>
	private async Task InitialiseCloudSaveServices()
	{
		try
		{
			await CloudSaveManager.Instance.LoadAndCacheData();

			if (string.IsNullOrEmpty(CloudSaveManager.Instance.playerStats.playerName))
			{
				Debug.Log($"Session Manager :: No Player Name found! Saving Player Name as '{GameSave.PlayerName}' to Cloud");
				await CloudSaveManager.Instance.SetPlayerName(GameSave.PlayerName);
			}
			
			Debug.Log($"Session Manager :: Using Player Name '{CloudSaveManager.Instance.playerStats.playerName}' from Cloud");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
	
	/// <summary>
	/// Requests to join a Relay Allocation using a Join Code
	/// Starts the Player as a Client
	/// </summary>
	internal async Task InitializeRelayClient(Lobby lobbyJoined)
	{
		try
		{
			// Extract the relay join code from lobby data
			if (lobbyJoined.Data.TryGetValue(LobbyManager.relayJoinCodeKey, out var relayData))
			{
				Debug.Log($"Found Join Code in Data: {relayData.Value}");
			}
			else
			{
				Debug.LogError("No relay join code found in lobby data!");
				return;
			}
			
			string relayJoinCode = relayData.Value;
			
			Debug.Log($"SessionManager :: RelayService Null?: {RelayService.Instance == null}");
			JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
			
			Debug.Log($"SessionManager :: Successfully joined relay allocation: {joinAllocation.AllocationId}");
			RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
			
			Instance.unityTransport.SetRelayServerData(relayServerData);
		
			NetworkManager.Singleton.StartClient();
			Debug.Log($"SessionManager :: Relay client started successfully");
		}
		catch (RelayServiceException e)
		{
			Debug.LogError($"SessionManager :: Relay Service Exception: {e.Message}, Reason: {e.Reason}");
			throw;
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Shuts down the NetworkManager and Unity Transport and yields when done
	/// </summary>
	/// <returns></returns>
	public async Task ShutdownNetwork()
	{
		// Shut down the Net Manager to clean up its state
		if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
		{
			NetworkManager.Singleton.Shutdown();
			
			// Wait until the Net Manager is shut down, to clear the transport data fully.
			// Not doing this means residual old data could exist, resulting in errors
			while (NetworkManager.Singleton.ShutdownInProgress)
			{
				await Task.Yield();
			}
        
			Debug.Log("Network shutdown complete, proceeding to create new host");
		}
		
		// Dispose of stale transport
		unityTransport.Shutdown();
	}

	/// <summary>
	/// Returns the Project Name
	/// </summary>
	private string GetProjectName()
	{
		string[] s = Application.dataPath.Split('/');
		string projectName = s[s.Length - 2];
		return projectName;
	}
}