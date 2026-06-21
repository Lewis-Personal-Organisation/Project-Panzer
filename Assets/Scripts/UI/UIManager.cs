using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
#if UNITY_EDITOR
	public Panel[] activePanels;
#endif
	
	private static Panel[] initialPanels;
	private static Stack<Panel> panelStack = new Stack<Panel>();
	public static Panel CurrentPanel => panelStack.Peek();
	
	public static View GameView { private set; get; }
	
	public DataStructs.GameResultsData previousGameResults { get; private set; }
	public bool arePreviousGameResultsSet { get; private set; }
	
	
	private new void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(this);

		// SceneData.LabelFixed("TEST LABEL", 10, 10);
		// SceneData.Label("Labels Count: ", $"{SceneData.labels.Count}", 10, 20);
		// SceneData.Texture(64,64, Color.cyan);
	}

	public void SetInitialPanels(params Panel[] panels)
	{
		panelStack.Clear();
		initialPanels = panels;
		PushPanels(initialPanels);
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
	public static void PushErrorScreen(string errMsg, NotifStyle msgStyle = NotifStyle.Info, float progressSpeed = 0.333333F, float fadeInSpeed = 5F, float fadeOutSpeed = 5F, float widthMulti = 0, float heightMulti = 0)
	{
		// Show and Fade in
		PreGameplayUI.FadedBackgroundUI.Fade(0, .75F, fadeInSpeed, () =>
		{
			PreGameplayUI.Notifs.PrepareStyleAndMessage(msgStyle, errMsg, widthMulti, heightMulti);
			PreGameplayUI.Notifs.StartProgressBar(0, progressSpeed, true, true, () =>
			{
				PreGameplayUI.FadedBackgroundUI.Fade(.75F, 0F, fadeOutSpeed, () =>
				{
					PopAllAndPush(initialPanels);
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
		panel.TogglePanels(true);

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
				currentPanel.TogglePanels(false);
			}
		}

		panelStack.Push(panel);
#if UNITY_EDITOR
		Instance.activePanels = panelStack.ToArray();
#endif
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
			panel.TogglePanels(false);
			
			// Debug.Log($"Popped {panel.GetPanel().name}");

			if (panelStack.Count == 0)
			{
				PushPanel(initialPanels[0]);
				return;
			}
			
			Panel newCurrentPanel = panelStack.Peek();
			
			if (newCurrentPanel.exitOnNewPagePush)
				newCurrentPanel.TogglePanels(true);
		}
		else
		{
			Debug.LogWarning("Trying to pop a page but only 1 page remains in the stack!");
		}
		
#if UNITY_EDITOR
		Instance.activePanels = panelStack.ToArray();
#endif
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
		PopUntil(initialPanels[0]);
		Debug.Log($"Popping until {initialPanels[0].GetPanel().name}");
		
		for (int i = 0; i < panels.Length; i++)
		{
			Debug.Log($"Pushing {panels[i].GetType().Name}");
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