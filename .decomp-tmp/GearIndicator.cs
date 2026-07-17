using UnityEngine;
using UnityEngine.UI;

public class GearIndicator : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private float speedLimit;

	[SerializeField]
	private Image brakeIcon;

	[SerializeField]
	private Image gearIcon;

	[SerializeField]
	private Sprite brakeSprite;

	[SerializeField]
	private Sprite parkingBrakeSprite;

	[SerializeField]
	private AudioClip raiseGearVoice;

	[SerializeField]
	private AudioClip lowerGearVoice;

	[SerializeField]
	private bool lowerGearWarning;

	[SerializeField]
	private Text lowerGearText;

	private float lowerGearLastPlayed = -45f;

	private bool raiseGearPlayed;

	private AircraftParameters aircraftParameters;

	private ControlInputs inputs;

	private void Awake()
	{
		lowerGearText.enabled = false;
	}

	public override void RefreshSettings()
	{
	}

	public override void Initialize(Aircraft aircraft)
	{
		speedLimit /= 3.6f;
		this.aircraft = aircraft;
		inputs = aircraft.GetInputs();
		aircraftParameters = aircraft.definition.aircraftParameters;
	}

	public override void Refresh()
	{
		if (aircraft.gearState == LandingGear.GearState.LockedExtended)
		{
			if (!gearIcon.enabled)
			{
				gearIcon.enabled = true;
			}
			if (aircraft.speed > speedLimit)
			{
				gearIcon.color = Color.red;
				if (!raiseGearPlayed)
				{
					SoundManager.PlayInterfaceOneShot(raiseGearVoice);
					raiseGearPlayed = true;
				}
			}
			else
			{
				raiseGearPlayed = false;
				gearIcon.color = Color.green;
			}
			bool flag = aircraft.speed < 1f && inputs.throttle < 0.1f;
			lowerGearText.enabled = false;
			brakeIcon.enabled = flag || inputs.brake > 0.02f;
		}
		else
		{
			raiseGearPlayed = false;
			gearIcon.color = Color.green;
			if (aircraft.gearState == LandingGear.GearState.LockedRetracted && gearIcon.enabled)
			{
				gearIcon.enabled = false;
				brakeIcon.enabled = false;
			}
			if (aircraft.gearState == LandingGear.GearState.Retracting || aircraft.gearState == LandingGear.GearState.Extending)
			{
				Image image = gearIcon;
				bool flag3 = (base.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f);
				image.enabled = flag3;
			}
		}
		if (lowerGearWarning && aircraft.gearState == LandingGear.GearState.LockedRetracted)
		{
			if (lowerGearText.enabled && Time.timeSinceLevelLoad - lowerGearLastPlayed > 4f)
			{
				lowerGearText.enabled = false;
			}
			if (inputs.throttle < 0.5f && aircraft.radarAlt < 30f && aircraft.rb.velocity.y < -0.5f && aircraft.speed < aircraftParameters.takeoffSpeed * 1.5f && Time.timeSinceLevelLoad - lowerGearLastPlayed > 60f)
			{
				lowerGearLastPlayed = Time.timeSinceLevelLoad;
				SoundManager.PlayInterfaceOneShot(lowerGearVoice);
				lowerGearText.enabled = true;
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
