using UnityEngine;
using UnityEngine.UI;

public class PropGauge : HUDApp
{
	[SerializeField]
	private string sourceName;

	[SerializeField]
	private Text rpmText;

	[SerializeField]
	private Text aoaText;

	[SerializeField]
	private Text powerText;

	[SerializeField]
	private Image propCircle;

	[SerializeField]
	private Gradient rpmColor;

	[SerializeField]
	private float warningThreshold;

	[SerializeField]
	private float maxRPM;

	private Aircraft aircraft;

	private ConstantSpeedProp source;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.gameObject.name == sourceName)
			{
				source = item.gameObject.GetComponent<ConstantSpeedProp>();
				break;
			}
		}
	}

	public override void Refresh()
	{
		if (!(source == null))
		{
			float rPMRatio = source.GetRPMRatio();
			rpmText.text = $"{rPMRatio * 100f:F1}%";
			aoaText.text = $"{source.GetAoA():F1}ø";
			powerText.text = UnitConverter.PowerReading(source.GetPowerAvailable() * 0.001f);
			Color color = rpmColor.Evaluate((rPMRatio - warningThreshold) / (1f - warningThreshold));
			rpmText.color = color;
			propCircle.fillAmount = rPMRatio;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
