using UnityEngine;
using UnityEngine.UI;

public class CustomAxis1Gauge : HUDApp
{
	[SerializeField]
	private Image negativeBar;

	[SerializeField]
	private Image positiveBar;

	[SerializeField]
	private float axisSplitPosition = 0.4f;

	[SerializeField]
	private float idleZone = 0.03f;

	[SerializeField]
	private Text label;

	private ControlInputs inputs;

	private Aircraft aircraft;

	private float axisPrev;

	public override void Initialize(Aircraft aircraft)
	{
		inputs = aircraft.GetInputs();
		this.aircraft = aircraft;
	}

	public override void Refresh()
	{
		if (!(aircraft == null) && axisPrev != inputs.customAxis1)
		{
			axisPrev = inputs.customAxis1;
			float num = axisSplitPosition + idleZone;
			float num2 = axisSplitPosition - idleZone;
			float fillAmount = (inputs.customAxis1 - num) / (1f - num);
			float fillAmount2 = (num2 - inputs.customAxis1) / num2;
			positiveBar.fillAmount = fillAmount;
			negativeBar.fillAmount = fillAmount2;
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		label.fontSize = (int)((float)(fontSize - 4) * fontSizeMultiplier);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
