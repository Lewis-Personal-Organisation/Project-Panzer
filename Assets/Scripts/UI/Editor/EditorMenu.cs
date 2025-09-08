using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

public class EditorMenu : MonoBehaviour
{
	private static EditorCoroutine screenshotHost = null;

	[MenuItem("Screenshots/Take Screenshot (Game View)")]
	private static void ScreenshotGameView()
	{
		if (screenshotHost != null)
		{
			Debug.LogWarning($"ScreenshotGameView :: A Screenshot is currently in progress. Returning...");
			return;
		}
		
		Camera gameCam = GetFirstActiveCamera();
		
		if (gameCam == null)
		{
			Debug.LogError($"ScreenshotGameView :: NO CAMERAS IN SCENE HIERARCHY...");
			return;
		}
		
		string folderPath = Application.dataPath + "/Screenshots/";
		string screenshotName = $"Screenshot {System.DateTime.Now.ToString("dd-MM-yyyy.HH-mm-ss")}.png";
		string path = System.IO.Path.Combine(folderPath, screenshotName);
		
		// Create Dir if it doesn't exist
		System.IO.Directory.CreateDirectory(folderPath);

		ScreenCapture.CaptureScreenshot(path, 1);
		screenshotHost = EditorCoroutineUtility.StartCoroutine(WaitForFile(path), gameCam);
	}

	// [MenuItem("Screenshots/Screenshot with Keybind (Start Listener) (Game View)")]
	// private static void ScreenshotGameViewWithKeybind()
	// {
	// 	if (screenshotHost != null)
	// 	{
	// 		Debug.LogWarning($"ScreenshotGameViewWithKeybind :: A Screenshot is currently in progress. Returning...");
	// 		return;
	// 	}
	// 	
	// 	Camera gameCam = GetFirstActiveCamera();
	// 	
	// 	if (gameCam == null)
	// 	{
	// 		Debug.LogError($"ScreenshotGameView :: NO CAMERAS IN SCENE HIERARCHY...");
	// 		return;
	// 	}
	//
	// 	screenshotHost = EditorCoroutineUtility.StartCoroutine(WaitForKeybindScreenshot(gameCam), gameCam);
	// }

	// private static IEnumerator WaitForKeybindScreenshot(Camera cam)
	// {
	// 	Debug.Log("WaitforKeybindScreenshot :: Waiting for Keybinds (S, P)");
	// 	float timeNow = DateTime.Today.Second + 5F;
	// 	yield return new WaitUntil(() => Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.P) || DateTime.Today.Second > timeNow);
	// 	Debug.Log("WaitforKeybindScreenshot :: Keybind hit! Taking Screenshot");
	// 	
	// 	string folderPath = Application.dataPath + "/Screenshots/";
	// 	string screenshotName = $"Screenshot {System.DateTime.Now.ToString("dd-MM-yyyy.HH-mm-ss")}.png";
	// 	string path = System.IO.Path.Combine(folderPath, screenshotName);
	// 	
	// 	// Create Dir if it doesn't exist
	// 	System.IO.Directory.CreateDirectory(folderPath);
	//
	// 	ScreenCapture.CaptureScreenshot(path, 1);
	// 	yield return EditorCoroutineUtility.StartCoroutine(WaitForFile(path), cam);
	// }
	
	private static IEnumerator WaitForFile(string path)
	{
		float timeNow = DateTime.Today.Second + 5F;
		yield return new WaitUntil(() => System.IO.File.Exists(path) || DateTime.Today.Second > timeNow);
		screenshotHost = null;
		AssetDatabase.Refresh();
		
		Debug.Log($"ScreenshotGameView :: Screenshot created! ({path})");
	}

	private static Camera GetFirstActiveCamera()
	{
		Camera[] allActiveCams = GameObject.FindObjectsOfType<Camera>();
		return allActiveCams.Length > 0 ? allActiveCams[0] : null;
	}

	[MenuItem("Game Data/Reset Player Name")]
	private static void ResetPlayerPrefs()
	{
		GameSave.PlayerName = string.Empty;
		Debug.Log($"EDITOR: PlayerName reset to '{GameSave.PlayerName}'");
	}
}