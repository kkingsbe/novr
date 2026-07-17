using UnityEngine;
using UnityEngine.UI;

public class AoADisplay : HUDApp
{
	[SerializeField]
	private Text AoAText;

	[SerializeField]
	private Text stallText;

	[SerializeField]
	private Gradient colorAtAngle;

	[SerializeField]
	private AudioClip stallHorn;

	[SerializeField]
	private AudioClip stallVoice;

	[SerializeField]
	private float hornVolume;

	[SerializeField]
	private float hornThreshold;

	[SerializeField]
	private float velocityThreshold = 10f;

	[SerializeField]
	private float gradientMax;

	private Aircraft aircraft;

	private AudioSource hornSource;

	private float hornLastPlayed;

	private void Awake()
	{
		stallText.enabled = false;
	}

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		hornSource = aircraft.gameObject.AddComponent<AudioSource>();
		hornSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		hornSource.clip = stallHorn;
		hornSource.loop = true;
		hornSource.volume = hornVolume;
		hornSource.spatialBlend = 0f;
		hornSource.dopplerLevel = 0f;
		hornSource.bypassEffects = true;
	}

	private void OnDestroy()
	{
		if (hornSource != null)
		{
			hornSource.Stop();
		}
	}

	private void OnDisable()
	{
		if (hornSource != null)
		{
			hornSource.Stop();
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		AoAText.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
		float num = Mathf.Atan2(vector.y, vector.z) * -57.29578f;
		AoAText.enabled = aircraft.speed > velocityThreshold;
		if (AoAText.enabled)
		{
			AoAText.color = colorAtAngle.Evaluate(num / gradientMax);
			AoAText.text = $"{num:F1}ø";
		}
		if (num > hornThreshold && AoAText.enabled)
		{
			stallText.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f;
			stallText.color = AoAText.color;
			if (!hornSource.isPlaying)
			{
				hornSource.Play();
				if (Time.timeSinceLevelLoad - hornLastPlayed > 5f)
				{
					SoundManager.PlayInterfaceOneShot(stallVoice);
				}
			}
			hornLastPlayed = Time.timeSinceLevelLoad;
		}
		else
		{
			stallText.enabled = false;
			if (hornSource.isPlaying)
			{
				hornSource.Stop();
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
