using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Text;

public enum TextSubmissionContext
{
	PlayerName,
	RelayJoinCode
}

public class TextInputUI : Panel
{
	private int MaxInputTextLength => textInputContext == TextSubmissionContext.PlayerName ? 10 : JoinCode.maxLength;

	[Header("Text Input")]
	[SerializeField] private TextSubmissionContext textInputContext;
	[SerializeField] private TextMeshProUGUI inputTitle;
	[SerializeField] private TextMeshProUGUI inputText;
	[SerializeField] private Button pasteButton;
	[SerializeField] private Button submitButton;
	[SerializeField] private Button closeButton;

	public string enteredJoinCode = string.Empty;

	// The callback for Update method
	private UnityAction OnUpdate;

	public void TogglePasteButton(bool state) => pasteButton.gameObject.SetActive(state);
	
	private readonly StringBuilder keyboardString = new StringBuilder(10);


	private void Awake()
	{
		submitButton.onClick.AddListener(SubmitTextInput);
		pasteButton.onClick.AddListener(OnPaste);
		closeButton.onClick.AddListener(() =>
		{
			UIManager.PopAllAndPush(PreGameplayUI.MainMenu);
		});

		onPopAction.AddListener(() => Prepare(false));
	}

	private void Update()
	{
		OnUpdate?.Invoke();
	}

	public Panel Prepare(bool isShowing, bool forceHideCloseButton = false, TextSubmissionContext context = TextSubmissionContext.PlayerName)
	{
		if (isShowing)
		{
			if (context == TextSubmissionContext.PlayerName)
			{
				inputTitle.text = "Enter a Username";
				
				// Text field could be empty if it used to input Relay code last
				if (inputText.text == string.Empty)
					inputText.text = GameSave.PlayerName;
				
				keyboardString.Clear();
				keyboardString.Append(GameSave.PlayerName);
				pasteButton.gameObject.SetActive(false);
				closeButton.gameObject.SetActive(false);
			}
			else
			{
				inputTitle.text = "Enter Join Code";
				inputText.text = string.Empty;
				pasteButton.gameObject.SetActive(true);
				closeButton.gameObject.SetActive(true);
			}

			textInputContext = context;
			OnUpdate += TakeKeyboardInput;
		}
		else
		{
			OnUpdate -= TakeKeyboardInput;
		}

		return this;
	}
	
	/// <summary>
	/// Filter the string inputs from Keyboard - letters/numbers allowed, Characters can be removed with backspace.
	/// Input can be submitted with Return.
	/// </summary>
	private void TakeKeyboardInput()
	{
		foreach (char chr in Input.inputString)
		{
			// Char add - If char is Letter/Number length is not more than 10, update text
			if (Char.IsLetterOrDigit(chr) && keyboardString.Length + 1 <= MaxInputTextLength)
			{
				keyboardString.Append(chr);
				inputText.text = keyboardString.ToString();
			}
			
			// Char remove - If char is backspace and we have minimum 1 char, remove char
			else if (chr == '\b' && keyboardString.Length > 0)
			{
				keyboardString.Length--;
				inputText.text = keyboardString.ToString();
			}
			
			// Submit - If Char is any return key, attempt to submit name
			else if (chr == '\r' && keyboardString.Length > 0)
			{
				SubmitTextInput();
			}
		}
	}
	
	public void SetInputTextAndSubmit(string text)
	{ 
		inputText.text = text;
		submitButton.onClick.Invoke();
	}

	/// <summary>
	/// Set the input text using the Windows Clipboard buffer
	/// </summary>
	private void OnPaste()
	{
		inputText.text = GUIUtility.systemCopyBuffer;
	}

	/// <summary>
	/// Submits Text Input for the PlayerName or RelayJoinCode
	/// </summary>
	public void SubmitTextInput()
	{
		if (inputText.text.Length == 0)
		{
			UIManager.PushPanel(PreGameplayUI.Notifs.PrepareErrorMsg("Text can't be empty"));
			return;
		}

		switch (textInputContext)
		{
			case TextSubmissionContext.PlayerName:
				PreGameplayUI.MainMenu.Prepare(inputText.text);
				GameSave.PlayerName = PreGameplayUI.MainMenu.nameDisplayText.text;
				
				UIManager.PopAllAndPush(PreGameplayUI.MainMenu);
				SceneData.Label("UI View: ", UIManager.CurrentPanel.GetPanel().name);

				if (LobbyManager.previouslyRefusedUsername)
				{
					LobbyManager.Instance.JoinPrivateLobbyAsClient(enteredJoinCode, GameSave.PlayerName);
				}
				break;
			
			case TextSubmissionContext.RelayJoinCode:
				Debug.Log($"Trying to Submit Text for lobby: '{inputText.text}'");
				if (JoinCode.IsValid(inputText.text.ToUpper()))
				{
					enteredJoinCode = inputText.text;
					LobbyManager.Instance.JoinPrivateLobbyAsClient(enteredJoinCode, GameSave.PlayerName);
					UIManager.PopPanel();
					inputText.text = string.Empty;

					UIManager.PopPanel(PreGameplayUI.Notifs);
				}
				else
				{
					UIManager.PushErrorScreen("Join Code or Code Format incorrect", NotifStyle.Error, 0.2F, 5, 5, 1.5F, 1F);
				}
				break;
		}
	}
}