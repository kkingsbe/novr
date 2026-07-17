using System;
using UnityEngine;
using UnityEngine.UI;

public class EngineTelemetry : MonoBehaviour
{
	[Serializable]
	private class Gauge
	{
		[SerializeField]
		private Text title;

		[SerializeField]
		private Text reading;

		[SerializeField]
		private Transform needle;

		[SerializeField]
		private float maxValue;

		public void UpdatePower(float power)
		{
			if (!(reading == null))
			{
				reading.text = (power * 0.001f).ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (power * 0.001f / maxValue) * -300f;
			}
		}

		public void UpdateNumber(float number)
		{
			if (!(reading == null))
			{
				reading.text = number.ToString("F0");
				needle.transform.localEulerAngles = Vector3.forward * (number / maxValue) * -300f;
			}
		}

		public void UpdateRPM(float rpmRatio)
		{
			if (!(reading == null))
			{
				reading.text = $"{rpmRatio * 100f:F0}%";
				needle.transform.localEulerAngles = Vector3.forward * rpmRatio * -300f;
			}
		}

		public void UpdateThrust(float thrust)
		{
			if (!(reading == null))
			{
				reading.text = (thrust * 0.001f).ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (thrust / maxValue) * -300f;
			}
		}
	}

	private Aircraft aircraft;

	[SerializeField]
	private string engineName;

	[SerializeField]
	private Gauge thrustGauge;

	[SerializeField]
	private Gauge rpmGauge;

	[SerializeField]
	private Gauge throttleGauge;

	[SerializeField]
	private Gauge pitchGauge;

	[SerializeField]
	private Text statusDisplay;

	[SerializeField]
	private Text[] colorableTexts;

	[SerializeField]
	private Image[] colorableImages;

	[SerializeField]
	private bool preferDisplayThrust;

	private IEngine engineInterface;

	private bool displayPower;

	private bool displayPitch;

	private float lastUpdate;

	private ControlInputs controlInputs;

	private void Start()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		foreach (DamageablePart damageable in aircraft.damageables)
		{
			Transform transform = damageable.Damageable.GetTransform();
			if (transform.gameObject.name == engineName && transform.gameObject.TryGetComponent<IEngine>(out engineInterface))
			{
				engineInterface.OnEngineDisable += EngineTelemetry_OnEngineFailure;
				engineInterface.OnEngineDamage += EngineTelemetry_OnEngineDamage;
				break;
			}
		}
		if (engineInterface == null)
		{
			Debug.LogWarning("Couldn't find engine interface for part " + engineName);
		}
		if (engineInterface is IPowerSource)
		{
			displayPower = true;
		}
		if (engineInterface is IPitchTelemetry)
		{
			displayPitch = true;
		}
		controlInputs = aircraft.GetInputs();
	}

	private void OnDestroy()
	{
		if (engineInterface != null)
		{
			engineInterface.OnEngineDisable -= EngineTelemetry_OnEngineFailure;
			engineInterface.OnEngineDamage -= EngineTelemetry_OnEngineDamage;
		}
	}

	private void EngineTelemetry_OnEngineFailure()
	{
		ApplyColor(Color.red);
		statusDisplay.text = "INOPERABLE";
	}

	private void EngineTelemetry_OnEngineDamage()
	{
		ApplyColor(Color.yellow);
		statusDisplay.text = "FAULT";
	}

	private void ApplyColor(Color color)
	{
		Text[] array = colorableTexts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].color = color;
		}
		Image[] array2 = colorableImages;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].color = color;
		}
		statusDisplay.color = color;
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - lastUpdate < 0.05f)
		{
			return;
		}
		lastUpdate = Time.timeSinceLevelLoad;
		if (!(engineInterface as UnityEngine.Object))
		{
			base.enabled = false;
			return;
		}
		if (displayPower)
		{
			if (engineInterface is IPowerSource powerSource)
			{
				float power = powerSource.GetPower();
				thrustGauge.UpdatePower(power);
			}
			if (preferDisplayThrust && engineInterface is IThrustSource thrustSource)
			{
				float thrust = thrustSource.GetThrust();
				thrustGauge.UpdateThrust(thrust);
			}
		}
		else if (engineInterface is IThrustSource thrustSource2)
		{
			float thrust2 = thrustSource2.GetThrust();
			thrustGauge.UpdateThrust(thrust2);
		}
		if (displayPitch)
		{
			pitchGauge.UpdateNumber((engineInterface as IPitchTelemetry).GetPitch());
		}
		rpmGauge.UpdateRPM(engineInterface.GetRPMRatio());
		throttleGauge.UpdateNumber(controlInputs.throttle * 100f);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
