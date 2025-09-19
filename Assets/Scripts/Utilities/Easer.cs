using UnityEngine;

public enum Ease
{
	None,
	Linear,
	InQuad,
	OutQuad,
	InOutQuad,
	InCubic,
	OutCubic,
	InOutCubic,
	InQuart,
	OutQuart,
	InOutQuart,
	InQuint,
	OutQuint,
	InOutQuint,
	InSine,
	OutSine,
	InOutSine,
	InExpo,
	OutExpo,
	InOutExpo,
	InCirc,
	OutCirc,
	InOutCirc,
	InElastic,
	OutElastic,
	InOutElastic,
	InBack,
	OutBack,
	InOutBack,
	InBounce,
	OutBounce,
	InOutBounce
}

public static class Easer
{
	/// <summary>
	/// Returns 0 if a value is NaN
	/// </summary>
	private static float FilterNaN(float v) => float.IsNaN(v) ? 0F : v;
	
	/// <summary>
	/// Returns the result of an Easing function given t <br/>
	/// Guarantees no NaN values
	/// </summary>
	public static float Calculate(Ease func, float t) => func switch
	{
		Ease.Linear => FilterNaN(Linear(t)),
		Ease.InQuad => FilterNaN(InQuad(t)),
		Ease.OutQuad => FilterNaN(OutQuad(t)),
		Ease.InOutQuad => FilterNaN(InOutQuad(t)),
		Ease.InCubic => FilterNaN(InCubic(t)),
		Ease.OutCubic => FilterNaN(OutCubic(t)),
		Ease.InOutCubic => FilterNaN(InOutCubic(t)),
		Ease.InQuart => FilterNaN(InQuart(t)),
		Ease.OutQuart => FilterNaN(OutQuart(t)),
		Ease.InOutQuart => FilterNaN(InOutQuart(t)),
		Ease.InSine => FilterNaN(InSine(t)),
		Ease.OutSine => FilterNaN(OutSine(t)),
		Ease.InOutSine => FilterNaN(InOutSine(t)),
		Ease.InExpo => FilterNaN(InExpo(t)),
		Ease.OutExpo => FilterNaN(OutExpo(t)),
		Ease.InOutExpo => FilterNaN(InOutExpo(t)),
		Ease.InCirc => FilterNaN(InCirc(t)),
		Ease.OutCirc => FilterNaN(OutCirc(t)),
		Ease.InOutCirc => FilterNaN(InOutCirc(t)),
		Ease.InElastic => FilterNaN(InElastic(t)),
		Ease.OutElastic => FilterNaN(OutElastic(t)),
		Ease.InOutElastic => FilterNaN(InOutElastic(t)),
		Ease.InBack => FilterNaN(InBack(t)),
		Ease.OutBack => FilterNaN(OutBack(t)),
		Ease.InOutBack => FilterNaN(InOutBack(t)),
		Ease.InBounce => FilterNaN(InBounce(t)),
		Ease.OutBounce => FilterNaN(OutBounce(t)),
		Ease.InOutBounce => FilterNaN(InOutBounce(t))
	};

	private static float Linear(float t) => t;
	private static float InQuad(float t) => t * t;
	private static float OutQuad(float t) => 1 - InQuad(1 - t);

	private static float InOutQuad(float t)
	{
		if (t < 0.5) return InQuad(t * 2) / 2;
		return 1 - InQuad((1 - t) * 2) / 2;
	}

	private static float InCubic(float t) => t * t * t;
	private static float OutCubic(float t) => 1 - InCubic(1 - t);

	private static float InOutCubic(float t)
	{
		if (t < 0.5) return InCubic(t * 2) / 2;
		return 1 - InCubic((1 - t) * 2) / 2;
	}

	private static float InQuart(float t) => t * t * t * t;
	private static float OutQuart(float t) => 1 - InQuart(1 - t);

	private static float InOutQuart(float t)
	{
		if (t < 0.5) return InQuart(t * 2) / 2;
		return 1 - InQuart((1 - t) * 2) / 2;
	}

	private static float InQuint(float t) => t * t * t * t * t;
	private static float OutQuint(float t) => 1 - InQuint(1 - t);

	private static float InOutQuint(float t)
	{
		if (t < 0.5) return InQuint(t * 2) / 2;
		return 1 - InQuint((1 - t) * 2) / 2;
	}

	private static float InSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);
	private static float OutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);
	private static float InOutSine(float t) => (Mathf.Cos(t * Mathf.PI) - 1) / -2;

	private static float InExpo(float t) => Mathf.Pow(2, 10 * (t - 1));
	private static float OutExpo(float t) => 1 - InExpo(1 - t);

	private static float InOutExpo(float t)
	{
		if (t < 0.5) return InExpo(t * 2) / 2;
		return 1 - InExpo((1 - t) * 2) / 2;
	}

	private static float InCirc(float t) => -(Mathf.Sqrt(1 - t * t) - 1);
	private static float OutCirc(float t) => 1 - InCirc(1 - t);

	private static float InOutCirc(float t)
	{
		if (t < 0.5) return InCirc(t * 2) / 2;
		return 1 - InCirc((1 - t) * 2) / 2;
	}

	private static float InElastic(float t) => 1 - OutElastic(1 - t);

	private static float OutElastic(float t)
	{
		const float p = 0.3f;
		return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
	}

	private static float InOutElastic(float t)
	{
		if (t < 0.5) return InElastic(t * 2) / 2;
		return 1 - InElastic((1 - t) * 2) / 2;
	}

	private static float InBack(float t)
	{
		const float s = 1.70158f;
		return t * t * ((s + 1) * t - s);
	}

	private static float OutBack(float t) => 1 - InBack(1 - t);

	private static float InOutBack(float t)
	{
		if (t < 0.5) return InBack(t * 2) / 2;
		return 1 - InBack((1 - t) * 2) / 2;
	}

	private static float InBounce(float t) => 1 - OutBounce(1 - t);

	private static float OutBounce(float t)
	{
		const float div = 2.75f;
		const float mult = 7.5625f;

		if (t < 1 / div)
		{
			return mult * t * t;
		}
		else if (t < 2 / div)
		{
			t -= 1.5f / div;
			return mult * t * t + 0.75f;
		}
		else if (t < 2.5 / div)
		{
			t -= 2.25f / div;
			return mult * t * t + 0.9375f;
		}
		else
		{
			t -= 2.625f / div;
			return mult * t * t + 0.984375f;
		}
	}

	private static float InOutBounce(float t)
	{
		if (t < 0.5) return InBounce(t * 2) / 2;
		return 1 - InBounce((1 - t) * 2) / 2;
	}
}