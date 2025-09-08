using TMPro;
using UnityEngine;

public class ErrorUI : Panel
{
	private const string errorPrefix = "ERROR: ";
	[SerializeField] private TextMeshProUGUI errorText;


	// public void SetErrorText(string errorText)
	// {
	// 	this.errorText.text = $"{errorPrefix}{errorText}";
	// }

	public Panel Prepare(string errorText)
	{
		this.errorText.text = $"{errorPrefix}{errorText}";
		return this;
	}
}
