using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UIManager;

public static class Utils
{
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

	public static IEnumerator LerpImageColours(params LerpGroup[] lerpGroups)
	{
		float t = 0;

		while (t < 1)
		{
			for (int i = 0; i < lerpGroups.Length; i++)
			{
				if (lerpGroups[i].image.color == lerpGroups[i].toColor)
					continue;

				lerpGroups[i].image.color = Color.Lerp(lerpGroups[i].fromColor, lerpGroups[i].toColor, t);
				t += Time.deltaTime / lerpGroups[i].time;
			}
			yield return null;
		}
	}
}