using System.Collections;
using System.Threading;
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

	public struct LerpGroup
	{
		public float time;
		public Image image;
		public Color fromColor;
		public Color toColor;

		public LerpGroup(float time, Image image, Color fromColor, Color toColor)
		{
			this.time = time; 
			this.image = image; 
			this.fromColor = fromColor; 
			this.toColor = toColor;
		}
	}

	public IEnumerator LerpImageColours(LerpGroup[] lerpGroups)
	{
		float t = 0;

		while (t < 1)
		{
			for (int i = 0; i < lerpGroups.Length; i++)
			{
				lerpGroups[i].image.color = Color.Lerp(lerpGroups[i].fromColor, lerpGroups[i].toColor, t);
				t += Time.deltaTime / lerpGroups[i].time;
			}
			yield return null;
		}

	}
}