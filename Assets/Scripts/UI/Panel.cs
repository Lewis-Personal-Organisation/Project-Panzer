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
	[SerializeField] private GameObject panel;
	
	public bool exitOnNewPagePush;
	[FormerlySerializedAs("postPopAction")] public UnityEvent onPopAction;
	[FormerlySerializedAs("prePushAction")] public UnityEvent onPushAction;
	
	
	public void Toggle(bool activeState)
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
		
		UIManager.Instance.UpdateGameView(view);
	}
	
	public GameObject GetPanel() => panel;
}