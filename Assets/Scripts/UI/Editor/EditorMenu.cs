using System.Collections;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;
using Unity.Collections;

public class EditorMenu : MonoBehaviour
{
	private static EditorCoroutine screenshotHost = null;

	[MenuItem("Screenshots/Take Screenshot (Game View)")]
	private static void ScreenshotGameView()
	{
		if (screenshotHost != null)
		{
			Debug.Log($"A Screenshot is currently in progress. Returning...");
			return;
		}

		string folderPath = Application.dataPath + "/Screenshots/";
		string screenshotName = $"Screenshot {System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}.png";

		// Create Dir if it doesn't exist
		if (!System.IO.Directory.Exists(folderPath))						
			System.IO.Directory.CreateDirectory(folderPath); 

		Debug.Log($"Capturing Screenshot...");
		ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folderPath, screenshotName), 1); // takes the sceenshot, the "2" is for the scaled resolution, you can put this to 600 but it will take really long to scale the image up
		screenshotHost = EditorCoroutineUtility.StartCoroutine(GetTextureFromPNG(System.IO.Path.Combine(folderPath, screenshotName), 10F), Camera.main.gameObject);
	}

	private static IEnumerator GetTextureFromPNG(string filePath, float timeout)
	{
		float failTime = Time.time + timeout;
		Debug.Log($"Waiting until Screenshot is saved...");

		while (Time.time < failTime)
		{
			if (!System.IO.File.Exists(filePath))
				yield return null;

			Debug.Log($"Found Screenshot. Downloading Texture...");
			Texture2D tex;

			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(filePath))
			{
				yield return uwr.SendWebRequest();

				if (uwr.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(uwr.error);
					yield break;
				}
				else
				{
					// Get downloaded texture
					tex = DownloadHandlerTexture.GetContent(uwr);
				}
			}

			byte[] bytes = tex.EncodeToPNG();
			System.IO.File.WriteAllBytes(filePath, bytes);
			Debug.Log($"File complete. Path: {filePath}");
			AssetDatabase.Refresh();
			screenshotHost = null;
		}
	}

	[MenuItem("Game Data/Reset Player Name")]
	private static void ResetPlayerPrefs()
	{
		GameSave.PlayerName = string.Empty;
		Debug.Log($"EDITOR: PlayerName reset to '{GameSave.PlayerName}'");
	}
}