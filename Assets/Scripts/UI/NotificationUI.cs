using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum NotifStyle
{
	Info,
	Error
}

public class NotificationUI : Panel
{
	private NotifStyle activeStyle = NotifStyle.Info;
	private const string errorPrefix = "ERROR: ";
	[SerializeField] private Color errorColor = Color.red;
	[SerializeField] private Color infoColor = Color.white;
	[SerializeField] private TextMeshProUGUI messageText;

	// Progress Bar
	[SerializeField] private Image progressImage;
	private float fillValue;

	/// <summary>
	/// Prepares the Error UI with its message
	/// </summary>
	public Panel PrepareErrorMsg(string errorText)
	{
		this.messageText.text = $"{errorPrefix}{errorText}";
		return this;
	}

	public void SetStyle(NotifStyle style)
	{
		activeStyle = style;
	}

	public NotificationUI PrepareStyleAndMessage(NotifStyle style, string message)
	{
		messageText.color = style switch
		{
			NotifStyle.Info => infoColor,
			NotifStyle.Error => errorColor
		};
		
		activeStyle = style;
		messageText.text = message;
		return this;
	}

	/// <summary>
	/// Starts the progress bar animation
	/// </summary>
	public void StartProgressBar(float target, float speed, bool reset = true, bool showPanel = true, UnityAction onComplete = null)
	{
		if (reset)
		{
			if (Mathf.Approximately(target, 0))
			{
				fillValue = 1;
				progressImage.fillAmount = fillValue;
			}
			else if (Mathf.Approximately(target, 1))
			{
				fillValue = 0;
				progressImage.fillAmount = fillValue;
			}
		}
		
		if (showPanel)
			Toggle(true);
		
		this.StopAllCoroutines();
		StartCoroutine(Animate(target, speed, onComplete));
	}

	/// <summary>
	/// Animates the progress bar using target and speed
	/// </summary>
	private IEnumerator Animate(float end, float speed, UnityAction onComplete = null)
	{
		while (!Mathf.Approximately(fillValue, end))
		{
			fillValue = Mathf.MoveTowards(fillValue, end, Time.deltaTime * speed);
			progressImage.fillAmount = fillValue;
			yield return null;
		}
		
		onComplete?.Invoke();
		
		Toggle(false);
	}
}
