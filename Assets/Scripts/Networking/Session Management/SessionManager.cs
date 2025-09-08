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
using Unity.Services.Relay.Models;
using UnityEngine.Events;

public class SessionManager : Singleton<SessionManager>
{
	[SerializeField] internal UnityTransport unityTransport;
	public string uniqueProfileString = string.Empty;
	private bool IsNetworkReady => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
	[Space(10)]
	[SerializeField] public UnityEvent OnAuthenticated;

	private new void Awake()
	{
		base.Awake();
	}

	private void Start() => DontDestroyOnLoad(this.gameObject);

	public async Task InitialiseAndAuthenticatePlayer()
	{
		if (IsNetworkReady)
			return;
		
		await SignIntoUnityServices();
		await AuthenticatePlayer();
		
		// Wait until Cloud Save Manager is ready
		await Task.Run(async () => 
		{
			while (!CloudSaveManager.Instance)
				await Task.Yield();
		});
		
		await InitialiseCloudSaveServices();
	}

	/// <summary>
	/// Initialises Unity Services.
	/// If in Editor, applies the appropriate profile string.
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

			if (this == null)
				return;

			string cloudSavePlayerName = CloudSaveManager.Instance.playerStats.playerName;

			if (string.IsNullOrEmpty(cloudSavePlayerName))
			{
				Debug.Log($"Session Manager :: No Player Name found! Saving Player Name as '{GameSave.PlayerName}' to Cloud");
				await CloudSaveManager.Instance.SetPlayerName(GameSave.PlayerName);
			}
			else
			{
				Debug.Log($"Session Manager :: Found Player Name '{cloudSavePlayerName}' from Cloud");
			}
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
			string relayJoinCode = lobbyJoined.Data[LobbyManager.relayJoinCodeKey].Value;
			// await InitializeClient(relayJoinCode);

			JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
			NetworkEndPoint endPoint = NetworkEndPoint.Parse(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port);

			Instance.unityTransport.SetClientRelayData(endPoint.Address.Split(':')[0], endPoint.Port,
				joinAllocation.AllocationIdBytes, joinAllocation.Key,
				joinAllocation.ConnectionData, joinAllocation.HostConnectionData, false);

			NetworkManager.Singleton.StartClient();
			Debug.Log($"Session Manager Relay Allocation complete. Starting as Client. Scene Sync: {NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled}");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	/// <summary>
	/// Shuts down the NetworkManager and yields when done
	/// </summary>
	/// <returns></returns>
	public IEnumerator IEShutdownNetworkClient()
	{
		NetworkManager.Singleton.Shutdown();
		yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
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