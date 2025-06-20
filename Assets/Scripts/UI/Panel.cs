using UnityEngine;

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
	public virtual void Toggle(bool activeState)
	{
		panel.SetActive(activeState);
		UIManager.Instance.UpdateGameView(view);
	}

	public bool panelIsActive => panel.activeSelf;
}