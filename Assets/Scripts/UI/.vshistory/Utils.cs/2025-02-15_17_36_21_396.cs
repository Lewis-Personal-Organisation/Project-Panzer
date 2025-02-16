using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{


	public IEnumerator ColourLerpOverTime(float time, ref Color baseColor, Color from, Color to)
	{
		float t = 0;

		while (t < time)
		{
			baseColor = Color.Lerp(from, to, t / time);

			yield return null;
		}
	}
}
