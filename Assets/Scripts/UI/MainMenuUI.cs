using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : Panel
{
	[Header("Game Connection")]
	[SerializeField] private Button hostGameButton;
	[SerializeField] private Button joinPrivateGameButton;
	[SerializeField] private Button joinPublicGameButton;
	[SerializeField] private Button quitButton;

	[Header("Player Name Input")]
	[SerializeField] internal TextMeshProUGUI nameDisplayText;
	[SerializeField] internal Button nameDisplayButton;
	public void ToggleNameDisplay(bool state) => nameDisplayText.gameObject.SetActive(state);


	private void Awake()
	{
		hostGameButton.onClick.AddListener(OnHostButtonPressed);
		joinPrivateGameButton.onClick.AddListener(OnJoinPrivateGameButtonPressed);
		//joinPublicGameButton.onClick.AddListener(OnJoinPublicGameButtonPressed);
		nameDisplayButton.onClick.AddListener(delegate { UIManager.TextInputGroup.ToggleInputTextGroup(true); });
#if UNITY_EDITOR
		quitButton.onClick.AddListener(EditorApplication.ExitPlaymode);
#else
		quitButton.onClick.AddListener(Application.Quit);
#endif
	}

	private void Start()
	{
		// Ask Player to set their name if not retrieved from Disk
		if (GameSave.PlayerName == string.Empty)
		{
			UIManager.TextInputGroup.ToggleInputTextGroup(true, TextSubmissionContext.PlayerName);
			UIManager.MainMenu.Toggle(false);
		}
		else
		{
			SetMainMenuName(GameSave.PlayerName);
		}
	}

	/// <summary>
	/// Set the Main Menu name text and visibility
	/// </summary>
	/// <param name="name"></param>
	/// <param name="show"></param>
	public void SetMainMenuName(string name)
	{
		nameDisplayText.text = name;
	}

	public override void Toggle(bool activeState)
	{
		base.Toggle(activeState);
	}

	/// <summary>
	/// Hides Game Connection buttons, shows the Lobby UI
	/// </summary>
	public void OnHostButtonPressed()
	{
		UIManager.LobbySetupMenu.Toggle(true);
		UIManager.LobbySetupMenu.SetLobbyNameText(nameDisplayText.text);
		Toggle(false);
	}


	/// <summary>
	/// Hide Game Connection buttons, show Text Input UI in RelayJoinCode context
	/// </summary>
	public void OnJoinPrivateGameButtonPressed()
	{
		UIManager.TextInputGroup.ToggleInputTextGroup(true, TextSubmissionContext.RelayJoinCode);
		Toggle(false);
	}

	/// <summary>
	/// UNFINISHED
	/// </summary>
	//public void OnJoinPublicGameButtonPressed()
	//{
	//	ToggleGameConnectionButtonVisiblity(false);
	//}
}