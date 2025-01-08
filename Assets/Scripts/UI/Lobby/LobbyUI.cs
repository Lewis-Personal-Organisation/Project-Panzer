using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LobbyManager;

public class LobbyUI : MonoBehaviour
{
	public static LobbyUI instance;

	[SerializeField] private GameObject parent;

	[SerializeField] private TextMeshProUGUI title;
	[SerializeField] private Button leaveButton;
	[SerializeField] private Button readyButton;
	[SerializeField] private TextMeshProUGUI readyButtonText;
	[SerializeField] private Color readyColour;
	[SerializeField] private Color unreadyColour;

	LobbyManager lobbyManager => LobbyManager.instance;
	bool isHost => lobbyManager.isHost;
	private bool isReady;

	[SerializeField] private PlayerSlot[] playerSlots = new PlayerSlot[4];

	[SerializeField]
	[System.Serializable]
	private class PlayerSlot
	{
		[SerializeField] private GameObject gameObject;
		[SerializeField] private Image backImage;
		[SerializeField] private TextMeshProUGUI playerNameTitle;
		[SerializeField] private GameObject readyGameObject;

		public bool IsFree() => !gameObject.activeInHierarchy;

		public void Show(string playerName)
		{
			playerNameTitle.text = playerName;
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			backImage.color = Color.white;
			playerNameTitle.text = string.Empty;
			gameObject.SetActive(false);
		}

		public void SetReady(bool isReady) => readyGameObject.SetActive(isReady);
	}


	private void Awake()
	{
		instance = this;
		leaveButton.onClick.AddListener(OnLeaveClicked);
		readyButton.onClick.AddListener(OnReadyClicked);
	}

	private void Start()
	{
		LobbyManager.OnLobbyChanged += OnLobbyChanged;
	}

	void OnLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		AdjustPlayerSlots();

		if (isHost)
		{
			OnHostLobbyChanged(updatedLobby, isGameReady);
		}
		else
		{
			OnJoinLobbyChanged(updatedLobby, isGameReady);
		}
	}

	public void OnHostLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		Debug.Log("OnLobbyChanged :: Host");
		//sceneView.SetHostLobbyPlayers(updatedLobby.Players);

		if (isGameReady)
		{
			// GAME IS READY TO START
			//NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
		}

		//LobbyManager.instance.LogLobbyPlayers();
	}

	public void OnJoinLobbyChanged(Lobby updatedLobby, bool isGameReady)
	{
		Debug.Log("OnLobbyChanged :: Client");
		//sceneView.SetJoinLobbyPlayers(updatedLobby.Players);
	}

	public void Hide()
	{
		parent.SetActive(false);
		Debug.Log($"This player left the Lobby and returned to main menu");
	}

	public void Show()
	{
		readyButton.interactable = true;
		leaveButton.interactable = true;

		parent.SetActive(true);
		SetLobbyTitle();
		AdjustPlayerSlots();
	}

	private void SetLobbyTitle()
	{
		title.text = isHost ? "Host Game Lobby" : "Game Lobby";
	}

	private async void OnReadyClicked()
	{
		try
		{
			readyButton.interactable = !readyButton.interactable;
			leaveButton.interactable = !leaveButton.interactable;

			isReady = !isReady;
			readyButton.image.color = isReady ? readyColour : unreadyColour;
			readyButtonText.text = isReady ? "Unready" : "Ready";

			await lobbyManager.SetReadyState(isReady);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			if (this != null)
			{
				readyButton.interactable = !readyButton.interactable;
				leaveButton.interactable = !leaveButton.interactable;
			}
		}
	}

	private async void OnLeaveClicked()
	{
		readyButton.interactable = false;
		leaveButton.interactable = false;

		if (lobbyManager.isHost)
		{
			await lobbyManager.DeleteAnyActiveLobbyWithNotify();
		}
		else
		{
			await lobbyManager.LeaveJoinedLobby();
		}
	}

	public void AdjustPlayerSlots()
	{
		List<Unity.Services.Lobbies.Models.Player> activePlayers = lobbyManager.GetLobbyPlayers();

		for (int i = 0; i < playerSlots.Length; i++)
		{
			if (i < activePlayers.Count)
			{
				playerSlots[i].Show(activePlayers[i].Data[PlayerDictionaryData.playerNameKey].Value);
				playerSlots[i].SetReady(bool.Parse(activePlayers[i].Data[PlayerDictionaryData.isReadyKey].Value));
			}
			else
			{
				playerSlots[i].Hide();
			}
		}
	}
}