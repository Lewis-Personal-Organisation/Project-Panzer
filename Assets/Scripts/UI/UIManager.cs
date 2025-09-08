using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : Singleton<UIManager>
{
	[FormerlySerializedAs("initialPanel")] [SerializeField] private Panel _initialPanel;
	private static Panel initialPanel;
	private static Stack<Panel> panelStack = new Stack<Panel>();
	
	public static View GameView { private set; get; }
	[Space(5)]
	[Header("Panels")]
	[SerializeField] private MainMenuUI mainMenu;
	[SerializeField] private LobbySetupUI lobbySetup;
	[SerializeField] private LobbyUI lobby;
	[SerializeField] private TextInputUI textInputGroup;

	[Header("Shared Panels")]
	[SerializeField] private FadedBackgroundUI fadedBackground;
	[SerializeField] private LoadingIcon loadingIcon;
	[SerializeField] private ErrorUI errorUI;
	
	public DataStructs.GameResultsData previousGameResults { get; private set; }
	
	public bool arePreviousGameResultsSet { get; private set; }
	public static LobbySetupUI LobbySetupMenu => Instance.lobbySetup;
	public static LobbyUI LobbyUI => Instance.lobby;
	public static MainMenuUI MainMenu => Instance.mainMenu;
	public static TextInputUI TextInputGroup => Instance.textInputGroup;
	public static LoadingIcon LoadingIcon => Instance.loadingIcon;
	public static FadedBackgroundUI FadedBackgroundUI => Instance.fadedBackground;
	public static ErrorUI ErrorUI => Instance.errorUI;
	
	
	private new void Awake()
	{
		base.Awake();
	}
	
	/// <summary>
	/// Sets the previous game results
	/// </summary>
	/// <param name="results"></param>
	public void SetPreviousGameResults(DataStructs.GameResultsData results)
	{
		previousGameResults = results;
		arePreviousGameResultsSet = true;
	}

	/// <summary>
	/// Update the Game View state
	/// </summary>
	/// <param name="view"></param>
	public void UpdateGameView(View view)
	{
		if (view == View.None)
			return;
		
		GameView = view;
	}

	/// <summary>
	/// Initialisation for UI stack system
	/// </summary>
	private void Start()
	{
		if (!_initialPanel)
		{
			Debug.LogError("Initial Panel not set!");
			return;
		}
		
		initialPanel = _initialPanel;
		PushPanel(initialPanel);
	}
	
	/// <summary>
	/// Push a panel to the stack and show it
	/// Optionally hide the current panel
	/// </summary>
	/// <param name="panel"></param>
	public static void PushPanel(Panel panel)
	{
		panel.Toggle(true);

		if (panelStack.Count > 0)
		{
			Panel currentPage = panelStack.Peek();

			if (currentPage.exitOnNewPagePush)
			{
				currentPage.Toggle(false);
			}
		}

		panelStack.Push(panel);
	}

	/// <summary>
	/// Attempts to Pop a panel if it matches the target. If a target is not specified, pops the current panel
	/// If no panels are left, pushes the initial panel
	/// </summary>
	/// <param name="target"></param>
	public static void PopPanel(Panel target = null)
	{
		// Optionally, only pop if this panel matches target
		if (target)
		{
			Debug.Log($"Trying to pop {target.GetPanel().name}. Current is {panelStack.Peek().GetPanel().name}");
			if (panelStack.Peek().GetPanel() != target.GetPanel())
				return;
		}

		if (panelStack.Count != 0)
		{
			Panel panel = panelStack.Pop();
			panel.Toggle(false);

			if (panelStack.Count == 0)
			{
				PushPanel(initialPanel);
				return;
			}
			
			Panel newCurrentPanel = panelStack.Peek();
			
			if (newCurrentPanel.exitOnNewPagePush)
				newCurrentPanel.Toggle(true);
		}
		else
		{
			Debug.LogWarning("Trying to pop a page but only 1 page remains in the stack!");
		}
	}

	/// <summary>
	/// Pops panels until the current panel matches the target
	/// Initial Panel is always shown if panel is never matched
	/// </summary>
	/// <param name="target"></param>
	public static void PopUntil(Panel target)
	{
		while (panelStack.Peek() != target)
		{
			PopPanel();
		}
	}

	/// <summary>
	/// Pops a specified amount of panels
	/// </summary>
	/// <param name="count"></param>
	public static void PopPanels(int count)
	{
		for (int i = 0; i < count; i++)
		{
			PopPanel();
		}
	}

	/// <summary>
	/// Pushes a number of panels to the stack in the given order
	/// </summary>
	/// <param name="panels"></param>
	public static void PushPanels(params Panel[] panels)
	{
		for (int i = 0; i < panels.Length; i++)
		{
			PushPanel(panels[i]);
		}
	}

	/// <summary>
	/// Pops and then pushes a mix of panels
	/// </summary>
	/// <param name="popCount"></param>
	/// <param name="panels"></param>
	public static void PopAndPush(int popCount, params Panel[] panels)
	{
		PopPanels(popCount);

		for (int i = 0; i < panels.Length; i++)
		{
			PushPanel(panels[i]);
		}
	}
	
	/// <summary>
	/// Pops all panels except the initial panel
	/// </summary>
	public static void PopAllPanels()
	{
		for (int i = 1; i < panelStack.Count; i++)
		{
			PopPanel();
		}
	}
}