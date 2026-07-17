using UnityEngine;
using UnityEngine.UI;

public class GIndicators : HUDApp
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Text gForceLabel;

	[SerializeField]
	private Text gMaxForceLabel;

	private float maxGNumber;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		gForceLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		gMaxForceLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			float num = Vector3.Dot(aircraft.pilots[0].GetAccel() + Vector3.up, aircraft.transform.up);
			maxGNumber = Mathf.Max(maxGNumber, num);
			gForceLabel.text = $"{num:F1}";
			gMaxForceLabel.text = $"[{maxGNumber:F1}]";
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
