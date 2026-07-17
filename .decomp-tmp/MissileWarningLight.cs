using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissileWarningLight : HUDApp
{
	[SerializeField]
	private Image[] images;

	private List<Missile> knownMissiles;

	private MissileWarning missileWarning;

	public override void Initialize(Aircraft aircraft)
	{
		base.enabled = false;
		missileWarning = aircraft.GetMissileWarningSystem();
		knownMissiles = missileWarning.knownMissiles;
		missileWarning.onMissileWarning += MissileWarningLights_OnMissileWarning;
		aircraft.onDisableUnit += MissileWarningLights_OnDisable;
	}

	private void MissileWarningLights_OnMissileWarning(MissileWarning.OnMissileWarning obj)
	{
		base.enabled = true;
	}

	private void MissileWarningLights_OnDisable(Unit unit)
	{
		missileWarning.onMissileWarning -= MissileWarningLights_OnMissileWarning;
		base.enabled = false;
	}

	public override void Refresh()
	{
		if (knownMissiles.Count > 0)
		{
			bool flag = Mathf.Sin(Time.timeSinceLevelLoad * 20f) > 0f;
			Image[] array = images;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = flag;
			}
		}
		else
		{
			base.enabled = false;
			Image[] array = images;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
