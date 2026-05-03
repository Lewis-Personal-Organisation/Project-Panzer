using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
	[SerializeField] private Panel _initialPanel;
	private static Panel initialPanel;
	private static Stack<Panel> panelStack = new Stack<Panel>();
	public static Panel CurrentPanel => panelStack.Peek();
	
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
	[SerializeField] private NotificationUI notificationUI;
	
	public DataStructs.GameResultsData previousGameResults { get; private set; }
	
	public bool arePreviousGameResultsSet { get; private set; }
	public static LobbySetupUI LobbySetupMenu => Instance.lobbySetup;
	public static LobbyUI LobbyUI => Instance.lobby;
	public static MainMenuUI MainMenu => Instance.mainMenu;
	public static TextInputUI TextInputGroup => Instance.textInputGroup;
	public static LoadingIcon LoadingIcon => Instance.loadingIcon;
	public static FadedBackgroundUI FadedBackgroundUI => Instance.fadedBackground;
	public static NotificationUI NotificationUI => Instance.notificationUI;
	
	
	private new void Awake()
	{
		base.Awake();
		
		if (!_initialPanel)
		{
			Debug.LogError("Initial Panel not set!");
			return;
		}
		
		initialPanel = _initialPanel;
		PushPanels(initialPanel, mainMenu);		// Initially, show game background and Main Menu

		SceneData.LabelFixed("TEST LABEL", 10, 10);
		SceneData.Label("Labels Count: ", $"{SceneData.labels.Count}", 10, 20);
		SceneData.Texture(64,64, Color.cyan);
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
	/// Shows the error screen with a message, timeout speed, fade speeds and size and then returns to the main menu
	/// </summary>
	public void PushErrorScreen(string errMsg, NotifStyle msgStyle = NotifStyle.Info, float progressSpeed = 0.333333F, float fadeInSpeed = 5F, float fadeOutSpeed = 5F, float widthMulti = 0, float heightMulti = 0)
	{
		// Show and Fade in
		FadedBackgroundUI.Fade(0, .75F, fadeInSpeed, () =>
		{
			NotificationUI.PrepareStyleAndMessage(msgStyle, errMsg, widthMulti, heightMulti);
			NotificationUI.StartProgressBar(0, progressSpeed, true, true, () =>
			{
				FadedBackgroundUI.Fade(.75F, 0F, fadeOutSpeed, () =>
				{
					PopAllAndPush(mainMenu);
				});
			});
		});
	}
	
	/// <summary>
	/// Push a panel to the stack and show it
	/// Optionally hide the current panel
	/// </summary>
	/// <param name="panel"></param>
	public static void PushPanel(Panel panel)
	{
		panel.Toggle(true);
		
		// Debug.Log($"Pushed {panel.GetPanel().name}. Count: {panelStack.Count}");
		
		if (panelStack.Count > 0)
		{
			Panel currentPanel = panelStack.Peek();

			// If we 
			if (panel == currentPanel)
				return;
			
			// Decide if the current shown panel can be shown simultaniously with the new panel.
			bool hideCurrent = true;
			
			for (int i = 0; i < currentPanel.newPagePushExcludedPanels.Length; i++)
			{
				if (panel == currentPanel.newPagePushExcludedPanels[i])
				{
					hideCurrent = false;
					// Debug.Log($"We can show {panel.GetPanel().name} above {currentPanel.GetPanel().name}!");
				}
			}
			
			// Only hide the panel if we dont want it shown 
			if (currentPanel.exitOnNewPagePush && hideCurrent)
			{
				currentPanel.Toggle(false);
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
			
			// Debug.Log($"Popped {panel.GetPanel().name}");

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
		if (target == null)
		{
			Debug.LogWarning("Trying to pop to a null panel!");
			return;
		}
		
		if (!panelStack.Contains(target))
		{
			Debug.LogWarning("Trying to pop to a panel which isn't on the stack");
		}
		
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
	/// Pops all panel until initial Panel and pushes panels
	/// </summary>
	/// <param name="panels"></param>
	public static void PopAllAndPush(params Panel[] panels)
	{
		PopUntil(initialPanel);
		Debug.Log($"Popping until {initialPanel.GetPanel().name}");
		
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

	public void LogPanelStack()
	{
		Panel[] stack = panelStack.ToArray();
		Array.Reverse(stack);
		
		for (int i = 0; i < stack.Length; i++)
		{
			Debug.Log($"Panel {i}: {stack[i]} on stack!");
		}
	}
}