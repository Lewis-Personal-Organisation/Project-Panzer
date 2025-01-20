using TMPro;
using UnityEngine;

public class LoadingIcon : Panel
{
	[SerializeField] private TextMeshProUGUI loadingText;


	public override void Toggle(bool activeState)
	{
		base.Toggle(activeState);

		if (activeState == false)
			loadingText.text = string.Empty;
	}

	public void SetText(string text)
	{
		loadingText.text = text;
	}

	public void ShowWithText(string text)
	{
		loadingText.text = text;
		Toggle(true);
	}
}