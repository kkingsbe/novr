using UnityEngine;
using UnityEngine.UI;

public class NozzleGauge : HUDApp
{
	[SerializeField]
	private Transform nozzleIcon;

	[SerializeField]
	private Transform inputPivot;

	[SerializeField]
	private Text label;

	[SerializeField]
	private float inputRange = -120f;

	[SerializeField]
	private bool fade;

	[SerializeField]
	private Image[] fadeImages;

	private INozzleGauge system;

	[SerializeField]
	private string sourceName;

	[SerializeField]
	private Transform HUDAnchor;

	private ControlInputs inputs;

	private float lastAngle;

	private float lastChange;

	private float opacity;

	private float[] imageAlphas;

	public override void Initialize(Aircraft aircraft)
	{
		inputs = aircraft.GetInputs();
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.gameObject.name == sourceName)
			{
				system = item.gameObject.GetComponent<INozzleGauge>();
				break;
			}
		}
		opacity = 1f;
		imageAlphas = new float[fadeImages.Length];
		for (int i = 0; i < fadeImages.Length; i++)
		{
			imageAlphas[i] = fadeImages[i].color.a;
		}
	}

	private void FadeOut()
	{
		opacity -= Time.deltaTime;
		if (opacity <= 0f)
		{
			Image[] array = fadeImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			label.enabled = false;
		}
		else
		{
			for (int j = 0; j < fadeImages.Length; j++)
			{
				fadeImages[j].color = new Color(0f, 1f, 0f, imageAlphas[j] * opacity);
			}
			label.color = new Color(0f, 1f, 0f, opacity);
		}
	}

	private void FadeIn()
	{
		if (opacity <= 0f)
		{
			Image[] array = fadeImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			label.enabled = true;
		}
		opacity += 3f * Time.deltaTime;
		opacity = Mathf.Clamp01(opacity);
		for (int j = 0; j < fadeImages.Length; j++)
		{
			fadeImages[j].color = new Color(0f, 1f, 0f, imageAlphas[j] * opacity);
		}
		label.color = new Color(0f, 1f, 0f, opacity);
	}

	public override void Refresh()
	{
		float nozzleAngle = system.GetNozzleAngle();
		if (nozzleAngle != lastAngle)
		{
			lastChange = Time.timeSinceLevelLoad;
		}
		lastAngle = nozzleAngle;
		if (fade)
		{
			if (Time.timeSinceLevelLoad - lastChange > 5f)
			{
				if (!(opacity > 0f))
				{
					return;
				}
				FadeOut();
			}
			else if (opacity < 1f)
			{
				FadeIn();
			}
		}
		inputPivot.localEulerAngles = new Vector3(0f, 0f, Mathf.Clamp01(1f - inputs.customAxis1) * inputRange);
		nozzleIcon.localEulerAngles = new Vector3(0f, 0f, 0f - nozzleAngle);
		label.text = $"NOZZLE {nozzleAngle:0}ø";
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
