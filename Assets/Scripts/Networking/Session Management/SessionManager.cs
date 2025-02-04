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

public class SessionManager : Singleton<SessionManager>
{
	[SerializeField] internal UnityTransport unityTransport;
	public bool useUnityServices = false;
	public string uniqueProfileString = string.Empty;
	public PlayerInfoData playerInfo;
	public bool networkManagerInitialised = true;
	private bool IsNetworkReady => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;


	[System.Serializable]
	public class PlayerInfoData
	{
		public DateTime? creationTime;
		public string ID;
		public string username;
	}

	new private void Awake()
	{
		base.Awake();
		GameSave.PrintPrefix();
	}

	public async Task InitialiseUnityServices()
	{
		try
		{
			if (IsNetworkReady)
				return;

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

		AuthenticationService.Instance.SignInFailed += (err) =>
		{
			playerInfo = null;
			Debug.LogError($"Unity Relay :: {err}");
		};
		AuthenticationService.Instance.SignedOut += () =>
		{
			playerInfo = null;
			Debug.Log($"Unity Relay :: Player signed out.");
		};
		AuthenticationService.Instance.Expired += () =>
		{
			playerInfo = null;
			Debug.Log($"Unity Relay :: Player session could not be refreshed and expired.");
		};
		AuthenticationService.Instance.SignedIn += () => Debug.Log($"Unity Relay :: Player Signed in. ID: {AuthenticationService.Instance.PlayerId}");

		try
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			playerInfo = new PlayerInfoData();
			playerInfo.creationTime = AuthenticationService.Instance.PlayerInfo.CreatedAt;
			playerInfo.ID = AuthenticationService.Instance.PlayerInfo.Id;
			playerInfo.username = AuthenticationService.Instance.PlayerInfo.Username;

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
			//await InitializeClient(relayJoinCode);

			var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
			var endPoint = NetworkEndPoint.Parse(joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port);

			var ipAddress = endPoint.Address.Split(':')[0];

			SessionManager.Instance.unityTransport.SetClientRelayData(ipAddress, endPoint.Port,
				joinAllocation.AllocationIdBytes, joinAllocation.Key,
				joinAllocation.ConnectionData, joinAllocation.HostConnectionData, false);

			//NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
			NetworkManager.Singleton.StartClient();
			Instance.networkManagerInitialised = true;
			Debug.Log("Relay Allocation complete. Starting as Client");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public IEnumerator IShutdownNetworkClient()
	{
		Debug.Log($"Network Client Shutting down....");
		NetworkManager.Singleton.Shutdown();
		yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
		Debug.Log($"Network Client Shutdown Succesfully");
		Instance.networkManagerInitialised = false;
	}

	private string GetProjectName()
	{
		string[] s = Application.dataPath.Split('/');
		string projectName = s[s.Length - 2];
		return projectName;
	}
}