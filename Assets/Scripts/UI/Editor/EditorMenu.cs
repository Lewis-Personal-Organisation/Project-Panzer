using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

public class EditorMenu : MonoBehaviour
{
	private static Coroutine Screenshot = null;

	[MenuItem("Screenshots/Take Screenshot (Game View)")]
	private static void ScreenshotGameView()
	{
		if (Screenshot != null)
			return;

		string folderPath = "Assets/Screenshots/"; // the path of your project folder

		if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
			System.IO.Directory.CreateDirectory(folderPath);  // it will get created

		string screenshotName = $"Screenshot {System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}.png"; // put youre favorite data format here
		ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folderPath, screenshotName), 1); // takes the sceenshot, the "2" is for the scaled resolution, you can put this to 600 but it will take really long to scale the image up
		Debug.Log(folderPath + screenshotName); // You get instant feedback in the console
		//Debug.Log(System.IO.File.Exists($"{folderPath}{screenshotName}.png"));
		//Screenshot = EditorCoroutineUtility.StartCoroutine(WaitForCaptureGeneration($"{folderPath}{screenshotName}.png"), this);
	}

	//private static IEnumerator WaitForCaptureGeneration(string filePath)
	//{
	//	yield return new WaitUntil(() => System.IO.File.Exists(filePath));
	//	AssetDatabase.Refresh();
	//	Screenshot = null;
	//}

	[MenuItem("Game Data/Reset Player Name")]
	private static void ResetPlayerPrefs()
	{
		GameSave.PlayerName = string.Empty;
		Debug.Log($"EDITOR: PlayerName reset to '{GameSave.PlayerName}'");
	}
}