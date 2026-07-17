using UnityEngine;
using UnityEngine.UI;

public class FuelGauge : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private Transform fuelReadingPivot;

	[SerializeField]
	private Text fuelReading;

	[SerializeField]
	private Text fuelLabel;

	[SerializeField]
	private Image fuelBar;

	[SerializeField]
	private Image fuelPointer;

	[SerializeField]
	private Image fuelArc;

	[SerializeField]
	private Gradient fuelGradient;

	private float lastReading;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		Show(PlayerSettings.gauges);
		Refresh();
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		fuelReading.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		fuelLabel.fontSize = (int)((float)(fontSize - 4) * fontSizeMultiplier);
		Show(PlayerSettings.gauges);
	}

	public override void Refresh()
	{
		if (!(aircraft == null) && !(Time.timeSinceLevelLoad - lastReading < 1f))
		{
			lastReading = Time.timeSinceLevelLoad;
			float fuelLevel = aircraft.GetFuelLevel();
			fuelReadingPivot.localEulerAngles = new Vector3(0f, 0f, 0f - (fuelLevel * 28f - 14f));
			fuelReading.transform.eulerAngles = new Vector3(0f, 0f, 0f - (SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z - aircraft.cockpit.transform.eulerAngles.z));
			fuelReading.text = (fuelLevel * 100f).ToString("F0") + "%";
			fuelBar.fillAmount = fuelLevel;
			fuelBar.color = fuelGradient.Evaluate(fuelLevel);
			fuelPointer.color = fuelBar.color;
			fuelLabel.color = fuelBar.color;
			fuelReading.color = fuelBar.color;
		}
	}

	public void Show(bool arg)
	{
		fuelArc.enabled = arg;
		fuelPointer.enabled = arg;
		fuelBar.enabled = arg;
		fuelReading.enabled = arg;
		fuelLabel.enabled = arg;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
