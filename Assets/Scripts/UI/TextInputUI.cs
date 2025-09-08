using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Diagnostics.Contracts;

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


	private void Awake()
	{
		submitButton.onClick.AddListener(SubmitTextInput);
		pasteButton.onClick.AddListener(OnPaste);
		closeButton.onClick.AddListener(() =>
		{
			UIManager.PopUntil(UIManager.MainMenu);
		});

		onPopAction.AddListener(() => Prepare(false));
	}

	private void Update()
	{
		OnUpdate?.Invoke();
	}

	public Panel Prepare(bool state, bool forceHideCloseButton = false, TextSubmissionContext context = TextSubmissionContext.PlayerName)
	{
		if (state)
		{
			inputText.text = string.Empty;
			if (context == TextSubmissionContext.PlayerName)
			{
				inputTitle.text = "Enter a Username";
				pasteButton.gameObject.SetActive(false);
				closeButton.gameObject.SetActive(false);
			}
			else
			{
				inputTitle.text = "Enter Join Code";
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
	/// Toggles the UI responsible for Text Input for Username etc.,
	/// </summary>
	// public void ToggleInputTextGroup(bool state, TextSubmissionContext context = TextSubmissionContext.PlayerName)
	// {
	// 	base.Toggle(state);
	// 	UIManager.FadedBackgroundUI.Toggle(state);
	//
	// 	if (state)
	// 	{
	// 		inputText.text = string.Empty;
	// 		if (context == TextSubmissionContext.PlayerName)
	// 		{
	// 			inputTitle.text = "Enter a Username";
	// 			pasteButton.gameObject.SetActive(false);
	// 		}
	// 		else
	// 		{
	// 			inputTitle.text = "Enter Join Code";
	// 			pasteButton.gameObject.SetActive(true);
	// 		}
	//
	// 		textInputContext = context;
	//
	// 		OnUpdate += TakeKeyboardInput;
	// 	}
	// 	else
	// 	{
	// 		OnUpdate -= TakeKeyboardInput;
	// 	}
	// }

	/// <summary>
	/// Filter the string inputs from Keyboard - letters/numbers allowed, Characters can be removed with backspace.
	/// Input can be submitted with Return.
	/// </summary>
	private void TakeKeyboardInput()
	{
		foreach (char chr in Input.inputString)
		{
			// If character is Letter or Number and the current length is not more than 10, update text
			if ((Char.IsLetter(chr) || Char.IsDigit(chr)) && inputText.text.Length + 1 <= MaxInputTextLength)
			{
				string temp = inputText.text + chr;
				inputText.text = temp;
			}
			// If Char is any return key, attempt to submit name
			else if (chr == '\r' && inputText.text.Length > 0)
			{
				SubmitTextInput();
			}
			// If char is backspace and we have minimum 1 char, remove char
			else if (chr == '\b' && inputText.text.Length > 0)
			{
				string temp = inputText.text.Remove(inputText.text.Length - 1);
				inputText.text = temp;
			}
		}
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
			UIManager.PushPanel(UIManager.ErrorUI.Prepare("Text can't be empty"));
			return;
		}

		switch (textInputContext)
		{
			case TextSubmissionContext.PlayerName:
				// UIManager.PushPanel(UIManager.MainMenu.Prepare(inputText.text));
				UIManager.MainMenu.Prepare(inputText.text);
				GameSave.PlayerName = UIManager.MainMenu.nameDisplayText.text;
				inputText.text = string.Empty;
				
				UIManager.PopUntil(UIManager.MainMenu);

				if (LobbyManager.previouslyRefusedUsername)
				{
					LobbyManager.Instance.JoinPrivateLobbyAsClient(enteredJoinCode, GameSave.PlayerName);
				}
				break;
			case TextSubmissionContext.RelayJoinCode:
				if (JoinCode.IsValid(inputText.text.ToUpper()))
				{
					enteredJoinCode = inputText.text;
					LobbyManager.Instance.JoinPrivateLobbyAsClient(enteredJoinCode, GameSave.PlayerName);
					UIManager.PopPanel();
					inputText.text = string.Empty;

					UIManager.PopPanel(UIManager.ErrorUI);
				}
				else
				{
					UIManager.PushPanel(UIManager.ErrorUI.Prepare("Entered Relay Code is incorrect. Try again."));
				}
				break;
		}
	}
}