using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Utils
{
	public static IEnumerator ColourLerp(float time, Image image, Color from, Color to)
	{
		float t = 0;

		while (t < 1)
		{
			image.color = Color.Lerp(from, to, t);
			t += Time.deltaTime / time;
			yield return null;
		}

		image.color = Color.Lerp(from, to, 1);
	}
}