using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Utils
{


	public static IEnumerator ColourLerpOverTime(float time, Image image, Color from, Color to)
	{
		float t = 0;

		while (t < time)
		{
			image.color = Color.Lerp(from, to, t / time);
			t += Time.deltaTime;
			yield return null;
		}

		image.color = Color.Lerp(from, to, 1);
	}
}
