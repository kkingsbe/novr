using UnityEngine;
using UnityEngine.UI;

public class MachIndicator : HUDApp
{
	private Aircraft aircraft;

	private AircraftParameters aircraftParameters;

	[SerializeField]
	private Text machDisplay;

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
		machDisplay.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			float speed = aircraft.speed;
			float speedOfSound = LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y);
			machDisplay.text = $"{speed / speedOfSound:F2}";
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
