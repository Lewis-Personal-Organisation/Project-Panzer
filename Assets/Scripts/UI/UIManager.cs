using System;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
	[SerializeField] private MainMenuUI mainMenu;
	[SerializeField] private LobbySetupUI lobbySetup;
	[SerializeField] private LobbyUI lobby;
	[SerializeField] private TextInputUI textInputGroup;

	[Header("Shared Objects")]
	[SerializeField] private FadedBackgroundUI fadedBackground;
	[SerializeField] private LoadingIcon loadingIcon;
	[SerializeField] private ErrorUI errorUI;
	
	public DataStructs.GameResultsData previousGameResults { get; private set; }
	public bool arePreviousGameResultsSet { get; private set; }
	public void SetPreviousGameResults(DataStructs.GameResultsData results)
	{
		previousGameResults = results;
		arePreviousGameResultsSet = true;
	}
	
	
	new private void Awake()
	{
		base.Awake();
	}
	
	public static LobbySetupUI LobbySetupMenu
	{
		get
		{
			return Instance.lobbySetup;
		}
	}
	public static LobbyUI LobbyUI
	{
		get
		{
			return Instance.lobby;
		}
	}
	public static MainMenuUI MainMenu
	{
		get
		{
			return Instance.mainMenu;
		}
	}
	public static TextInputUI TextInputGroup
	{
		get
		{
			return Instance.textInputGroup;
		}
	}
	public static LoadingIcon LoadingIcon
	{
		get
		{
			return Instance.loadingIcon;
		}
	}
	public static FadedBackgroundUI FadedBackgroundUI
	{
		get
		{
			return Instance.fadedBackground;
		}
	}
	public static ErrorUI ErrorUI
	{
		get
		{
			return Instance.errorUI;
		}
	}
}