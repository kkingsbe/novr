using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBox : MonoBehaviour
{
	[SerializeField]
	private GameObject holder;

	[SerializeField]
	private TextMeshProUGUI titleText;

	[SerializeField]
	private TextMeshProUGUI bodyText;

	[SerializeField]
	private TextMeshProUGUI buttonText;

	[SerializeField]
	private Button button;

	public int? CurrentId { get; private set; }

	public event Action<int> ButtonPressed;

	private void Awake()
	{
		button.onClick.AddListener(InvokeButtonPress);
	}

	private void InvokeButtonPress()
	{
		if (CurrentId.HasValue)
		{
			button.interactable = false;
			this.ButtonPressed(CurrentId.Value);
		}
		else
		{
			Debug.LogWarning("DialogueBox has no Id so button will do nothing");
		}
	}

	public void Show(int id, string title, string body, string button)
	{
		CurrentId = id;
		if (string.IsNullOrEmpty(title))
		{
			title = "Dialogue";
		}
		titleText.text = title;
		bodyText.text = body;
		buttonText.text = button;
		this.button.interactable = true;
		EnableBox(show: true);
	}

	public void Hide()
	{
		CurrentId = null;
		titleText.text = "";
		EnableBox(show: false);
	}

	private void EnableBox(bool show)
	{
		holder.SetActive(show);
		GameplayUI.AllowPauseKeybind = !show;
		GameManager.flightControlsEnabled = !show;
		CursorManager.SetFlag(CursorFlags.Dialogue, show);
		if (GameManager.gameState == GameState.SinglePlayer)
		{
			TimeScaleManager.Scale = (show ? 0f : (GameplayUI.GameSlowMotion ? 0.05f : 1f));
			AudioListener.pause = show;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
