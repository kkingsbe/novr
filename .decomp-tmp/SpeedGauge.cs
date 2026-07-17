using UnityEngine;
using UnityEngine.UI;

public class SpeedGauge : HUDApp
{
	private Aircraft aircraft;

	private AircraftParameters aircraftParameters;

	[SerializeField]
	private AudioClip overspeedVoice;

	[SerializeField]
	private Gradient speedGradient;

	[SerializeField]
	private Text airspeedDisplay;

	[SerializeField]
	private Text overspeedDisplay;

	[SerializeField]
	private Image border;

	[SerializeField]
	private float overspeedThreshold = float.MaxValue;

	private float lastOverspeed = -100f;

	private void Awake()
	{
		overspeedDisplay.enabled = false;
	}

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
			aircraftParameters = aircraft.definition.aircraftParameters;
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		airspeedDisplay.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		float speed = aircraft.speed;
		airspeedDisplay.text = UnitConverter.SpeedReading(speed);
		if (!(speed > overspeedThreshold))
		{
			if (overspeedDisplay.enabled)
			{
				overspeedDisplay.enabled = false;
			}
			airspeedDisplay.color = ((aircraft.gearState == LandingGear.GearState.LockedExtended) ? Color.green : speedGradient.Evaluate(speed * 0.5f / Mathf.Max(aircraftParameters.takeoffSpeed, 1f)));
			return;
		}
		overspeedDisplay.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f;
		if (Time.timeSinceLevelLoad - lastOverspeed > 20f)
		{
			SoundManager.PlayInterfaceOneShot(overspeedVoice);
		}
		lastOverspeed = Time.timeSinceLevelLoad;
		airspeedDisplay.color = Color.red;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
