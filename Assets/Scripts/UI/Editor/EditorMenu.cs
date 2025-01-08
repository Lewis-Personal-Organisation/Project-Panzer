using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorMenu : MonoBehaviour
{

	[MenuItem("Screenshots/Take Screenshot (Game View)")]
	private static void ScreenshotGameView()
	{
		string folderPath = "Assets/Screenshots/"; // the path of your project folder

		if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
			System.IO.Directory.CreateDirectory(folderPath);  // it will get created

		string screenshotName = $"Screenshot {System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}.png"; // put youre favorite data format here
		ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folderPath, screenshotName), 1); // takes the sceenshot, the "2" is for the scaled resolution, you can put this to 600 but it will take really long to scale the image up
		Debug.Log(folderPath + screenshotName); // You get instant feedback in the console
	}
}