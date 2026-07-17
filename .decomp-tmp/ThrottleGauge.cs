using System;
using UnityEngine;
using UnityEngine.UI;

public class ThrottleGauge : HUDApp
{
	[Serializable]
	private class ThrottleRegion
	{
		[SerializeField]
		private string name;

		[SerializeField]
		private bool showName;

		[SerializeField]
		private bool showPercent;

		[SerializeField]
		private float start;

		[SerializeField]
		private float end;

		[SerializeField]
		private Gradient gradient;

		private float percent;

		public bool IsActive(float input)
		{
			if (input >= start)
			{
				return input <= end;
			}
			return false;
		}

		public float GetPercent(float input)
		{
			percent = ((end - start > 0f) ? ((input - start) / (end - start)) : 1f);
			return percent;
		}

		public Color GetColor()
		{
			return gradient.Evaluate(percent);
		}

		public string GetText()
		{
			string text = string.Empty;
			if (showName)
			{
				text = name;
			}
			if (showPercent)
			{
				text += $"{percent * 100f:0}%";
			}
			return text;
		}

		public float GetStart()
		{
			return start;
		}

		public float GetEnd()
		{
			return end;
		}
	}

	[SerializeField]
	private Image throttleBar;

	[SerializeField]
	private Image throttleArc;

	[SerializeField]
	private Image throttlePointer;

	[SerializeField]
	private Text throttleReading;

	[SerializeField]
	private Text throttleLabel;

	[SerializeField]
	private Gradient throttleGradient;

	[SerializeField]
	private Color afterburnerColor;

	[SerializeField]
	private Transform throttleReadingPivot;

	[SerializeField]
	private Transform throttleBoundaryPivot;

	private ControlInputs inputs;

	[SerializeField]
	private bool airbrake;

	[SerializeField]
	private bool afterburner;

	[SerializeField]
	private ThrottleRegion[] throttleRegions;

	private ThrottleRegion currentRegion;

	private Aircraft aircraft;

	private float throttlePrev = -1f;

	public override void Initialize(Aircraft aircraft)
	{
		inputs = aircraft.GetInputs();
		this.aircraft = aircraft;
		throttlePrev = -1f;
		if (throttleBoundaryPivot != null && throttleRegions.Length != 0 && afterburner)
		{
			throttleBoundaryPivot.localEulerAngles = new Vector3(0f, 0f, (throttleRegions[^1].GetStart() + 0.01f) * 26f - 13f);
		}
		Show(PlayerSettings.gauges);
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		throttleReading.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		throttleLabel.fontSize = (int)((float)(fontSize - 4) * fontSizeMultiplier);
		Show(PlayerSettings.gauges);
	}

	public override void Refresh()
	{
		if (aircraft == null || throttlePrev == inputs.throttle)
		{
			return;
		}
		throttlePrev = inputs.throttle;
		throttleReadingPivot.localEulerAngles = new Vector3(0f, 0f, inputs.throttle * 26f - 13f);
		float z = SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z;
		float z2 = aircraft.cockpit.transform.eulerAngles.z;
		throttleReading.transform.eulerAngles = new Vector3(0f, 0f, 0f - (z - z2));
		throttleBar.fillAmount = inputs.throttle;
		if (throttleRegions.Length != 0)
		{
			ThrottleRegion[] array = throttleRegions;
			foreach (ThrottleRegion throttleRegion in array)
			{
				if (throttleRegion.IsActive(inputs.throttle))
				{
					currentRegion = throttleRegion;
				}
			}
		}
		if (currentRegion != null)
		{
			currentRegion.GetPercent(inputs.throttle);
			Color color = currentRegion.GetColor();
			throttleBar.color = color;
			throttleReading.color = color;
			throttleReading.text = currentRegion.GetText();
			return;
		}
		throttleReading.text = $"{inputs.throttle * 100f:F0}%";
		throttleBar.color = throttleGradient.Evaluate(inputs.throttle);
		throttleReading.color = throttleBar.color;
		if (afterburner && inputs.throttle == 1f)
		{
			throttleReading.text = "AFTERBURNER";
			throttleReading.color = afterburnerColor;
			throttleBar.color = afterburnerColor;
		}
		if (airbrake && inputs.throttle == 0f)
		{
			throttleReading.text = "AIRBRAKE";
		}
	}

	public void Show(bool arg)
	{
		throttleArc.enabled = arg;
		throttleBar.enabled = arg;
		throttlePointer.enabled = arg;
		throttleLabel.enabled = arg;
		throttleReading.enabled = arg;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
