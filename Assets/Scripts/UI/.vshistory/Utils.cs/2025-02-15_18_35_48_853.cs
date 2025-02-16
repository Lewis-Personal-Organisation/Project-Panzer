using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Utils
{
	public static IEnumerator ColourLerp(float time, Image image, Color from, Color to)
	{
		float t = 0;

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

		sw.Start();
		while (t < 1)
		{
			image.color = Color.Lerp(from, to, t);
			t += Time.deltaTime / time;
			Debug.Log($"{t}, {sw.ElapsedMilliseconds}");
			yield return null;
		}
		sw.Stop();

		image.color = Color.Lerp(from, to, 1);
	}
}