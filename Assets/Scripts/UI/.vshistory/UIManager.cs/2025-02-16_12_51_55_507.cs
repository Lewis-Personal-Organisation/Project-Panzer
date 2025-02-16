using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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


	new private void Awake()
	{
		base.Awake();
	}

	public IEnumerator LerpImageColours(float[] times, Image[] images, Color[] from, Color[] to)
	{
		float[] t = new float[times.Length];

		while (t[0] < 1)
		{
			for (int i = 0; i < images.Length; i++)
			{
				images[i].color = Color.Lerp(from[i], to[i], t[i]);
				t[i] += Time.deltaTime / times[i];
			}
			yield return null;
		}

	}
}