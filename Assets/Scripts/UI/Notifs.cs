using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum NotifStyle
{
	Info,
	Error
}

public class Notifs : Panel
{
	private NotifStyle activeStyle = NotifStyle.Info;
	private const string errorPrefix = "ERROR: ";
	[SerializeField] private Color errorColor = Color.red;
	[SerializeField] private Color infoColor = Color.white;
	[SerializeField] private TextMeshProUGUI messageText;
	[SerializeField] private RectTransform rect;

	private float defaultWidth, defaultHeight;
	
	
	[SerializeField] private Image progressImage; // Progress Bar
	private float fillValue;

	private void Awake()
	{
		defaultWidth = rect.sizeDelta.x;
		defaultHeight = rect.sizeDelta.y;
	}

	/// <summary>
	/// Prepares the Error UI with its message
	/// </summary>
	public Panel PrepareErrorMsg(string errorText)
	{
		messageText.text = $"{errorPrefix}{errorText}";
		return this;
	}

	/// <summary>
	/// Sets the current notification style
	/// </summary>
	/// <param name="style"></param>
	public void SetStyle(NotifStyle style)
	{
		activeStyle = style;
	}

	/// <summary>
	/// Prepares the Style and Message of the notification
	/// </summary>
	public Notifs PrepareStyleAndMessage(NotifStyle style, string message, float widthMulti = 0, float heightMulti = 0)
	{
		messageText.color = style switch
		{
			NotifStyle.Info => infoColor,
			NotifStyle.Error => errorColor
		};
		
		activeStyle = style;
		messageText.text = message;
		
		rect.sizeDelta = new Vector2(widthMulti != 0 ? defaultWidth * widthMulti : defaultWidth, 
									heightMulti != 0 ? defaultHeight * heightMulti : defaultHeight);
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
			TogglePanels(true);
		
		StopAllCoroutines();
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
		
		TogglePanels(false);
	}
}