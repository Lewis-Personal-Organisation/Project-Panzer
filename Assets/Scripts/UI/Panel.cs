using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum View
{
	MainMenu,
	LobbySetup,
	Lobby,
	Gameplay,
	None
}
public class Panel : MonoBehaviour
{
	[field: SerializeField] public View view { private set; get; }
	[FormerlySerializedAs("panels")]
	[SerializeField] protected GameObject panel;
	
	public bool exitOnNewPagePush;
	public Panel[] newPagePushExcludedPanels = Array.Empty<Panel>();
	[FormerlySerializedAs("postPopAction")] public UnityEvent onPopAction;
	[FormerlySerializedAs("prePushAction")] public UnityEvent onPushAction;
	
	public GameObject GetPanel() => panel;
	protected bool invertedState => !panel.activeSelf;
	
	/// <summary>
	/// Toggles the visibility of the panel and activates any actions
	/// </summary>
	public virtual void TogglePanels(bool activeState)
	{
		if (panel.activeSelf == activeState)
			return;

		panel.SetActive(activeState);
		
		if (activeState)
		{
			onPushAction.Invoke();
		}
		else
		{
			onPopAction.Invoke();
		}
		
		// Unused for now
		// UIManager.Instance?.UpdateGameView(view);
	}
}