using UnityEngine;

public static class GameSave
{
#if UNITY_EDITOR
	private static string basePrefix = ParrelSync.ClonesManager.IsClone() ? "Clone_" : string.Empty;
#else
	private static string basePrefix = SystemInfo.deviceUniqueIdentifier;
#endif

	private static string playerName = string.Empty;
	public static string PlayerName
	{
		get
		{
			if (playerName == string.Empty)
				playerName = PlayerPrefs.GetString($"{basePrefix}PlayerUsername", string.Empty);

			return playerName;
		}
		set
		{
			playerName = value;
			PlayerPrefs.SetString($"{basePrefix}PlayerUsername", playerName);
			PlayerPrefs.Save();
		}
	}

	public static void PrintPrefix()
	{
		Debug.Log($"Project is {(ParrelSync.ClonesManager.IsClone() ? "Clone" : "Original")}. Base Prefix is '{basePrefix}'. E.g '{basePrefix}PlayerUsername'");
	}
}