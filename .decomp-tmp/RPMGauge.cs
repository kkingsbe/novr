using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RPMGauge : HUDApp
{
	[SerializeField]
	private string[] sourceNames;

	[SerializeField]
	private Text rpmText;

	[SerializeField]
	private Image textBox;

	[SerializeField]
	private Gradient rpmColor;

	[SerializeField]
	private AudioClip warningSound;

	[SerializeField]
	private float warningVolume;

	[SerializeField]
	private float warningThreshold;

	[SerializeField]
	private float maxRPM;

	private Aircraft aircraft;

	private List<IEngine> sources;

	private AudioSource warningSource;

	private float displayedRPM;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		sources = new List<IEngine>();
		string[] array = sourceNames;
		foreach (string text in array)
		{
			foreach (UnitPart item in aircraft.partLookup)
			{
				if (item != null && item.gameObject.name == text)
				{
					sources.Add(item.gameObject.GetComponent<IEngine>());
					break;
				}
			}
		}
		warningSource = aircraft.gameObject.AddComponent<AudioSource>();
		warningSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		warningSource.clip = warningSound;
		warningSource.loop = true;
		warningSource.volume = warningVolume;
		warningSource.spatialBlend = 0f;
		warningSource.dopplerLevel = 0f;
		warningSource.bypassEffects = true;
	}

	private void OnDestroy()
	{
		if (warningSource != null)
		{
			warningSource.Stop();
		}
	}

	public override void Refresh()
	{
		displayedRPM = 0f;
		foreach (IEngine source in sources)
		{
			displayedRPM += source.GetRPM();
		}
		displayedRPM /= sources.Count;
		rpmText.text = $"RPM {displayedRPM:F0}";
		if (warningSource != null)
		{
			if (displayedRPM < warningThreshold && aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
			{
				if (!warningSource.isPlaying)
				{
					warningSource.Play();
				}
				warningSource.pitch = displayedRPM / warningThreshold;
			}
			else if (warningSource.isPlaying)
			{
				warningSource.Stop();
			}
		}
		Color color = rpmColor.Evaluate((displayedRPM - warningThreshold) / (maxRPM - warningThreshold));
		rpmText.color = color;
		textBox.color = color;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
