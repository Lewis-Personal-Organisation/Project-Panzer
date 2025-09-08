using TMPro;
using UnityEngine;

public class LoadingIcon : Panel
{
	[SerializeField] private TextMeshProUGUI loadingText;

	
	public Panel SetText(string text)
	{
		loadingText.text = text;
		return this;
	}

	public Panel Prepare(string text)
	{
		loadingText.text = text;
		return this;
	}
}