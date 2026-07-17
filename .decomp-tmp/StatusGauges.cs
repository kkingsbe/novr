using System;
using UnityEngine;
using UnityEngine.UI;

public class StatusGauges : HUDApp
{
	[Serializable]
	private class Gauge
	{
		[SerializeField]
		private Text title;

		[SerializeField]
		private Image image;

		[SerializeField]
		private Image circle;

		[SerializeField]
		private Text reading;

		[SerializeField]
		private Transform needle;

		[SerializeField]
		private float maxValue;

		public void Update(float value)
		{
			if (!(reading == null))
			{
				reading.text = value.ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (value / maxValue) * -300f;
				circle.fillAmount = 0.84f * value / maxValue;
				Color color = GameAssets.i.redGreenGradient.Evaluate(1f - value / maxValue);
				circle.color = color;
			}
		}
	}

	private Aircraft aircraft;

	[SerializeField]
	private Image fuelLevelDisplay;

	[SerializeField]
	private Image throttleLevelDisplay;

	[SerializeField]
	private Gauge irLevelGauge;

	[SerializeField]
	private Text massValue;

	[SerializeField]
	private Text twrValue;

	private IRSource irSource;

	private ControlInputs inputs;

	private float lastRefresh;

	private float refreshDelay = 10f;

	private float gaugeThickness = 25f;

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
			irSource = aircraft.GetIRSource();
			inputs = aircraft.GetInputs();
			float fuelLevel = aircraft.GetFuelLevel();
			fuelLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * fuelLevel);
			fuelLevelDisplay.color = GameAssets.i.redGreenGradient.Evaluate(fuelLevel);
			float mass = aircraft.GetMass();
			massValue.text = UnitConverter.WeightReading(mass);
		}
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		float value = Mathf.Clamp(irSource.intensity, 0f, 12f);
		irLevelGauge.Update(value);
		throttleLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * inputs.throttle);
		if (Time.timeSinceLevelLoad > lastRefresh + refreshDelay)
		{
			float fuelLevel = aircraft.GetFuelLevel();
			fuelLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * fuelLevel);
			fuelLevelDisplay.color = GameAssets.i.redGreenGradient.Evaluate(fuelLevel);
			float mass = aircraft.GetMass();
			massValue.text = UnitConverter.WeightReading(mass);
			float maxThrust;
			if (aircraft.GetMaxPower(out var maxPower))
			{
				twrValue.text = UnitConverter.PowerToWeightReading(maxPower * 0.001f / mass);
			}
			else if (aircraft.GetMaxThrust(out maxThrust))
			{
				twrValue.text = $"{maxThrust / (mass * 9.81f):F2}";
			}
			lastRefresh = Time.timeSinceLevelLoad;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
