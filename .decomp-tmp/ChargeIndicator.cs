using UnityEngine;
using UnityEngine.UI;

public class ChargeIndicator : MonoBehaviour
{
	[SerializeField]
	private Image chargeBar;

	[SerializeField]
	private Gradient barGradient;

	[SerializeField]
	private Text readout;

	private PowerSupply powerSupply;

	private float barLength;

	private float barHeight;

	private void Awake()
	{
		barLength = chargeBar.rectTransform.sizeDelta.x;
		barHeight = chargeBar.rectTransform.sizeDelta.y;
		CombatHUD.onSetAircraft += ChargeIndicator_OnSetAircraft;
	}

	private void Start()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			UpdateDisplay();
		}
	}

	private void ChargeIndicator_OnSetAircraft(CombatHUD sender)
	{
		if (powerSupply != null)
		{
			powerSupply.onChargeChanged -= ChargeIndicator_OnChargeChanged;
		}
		powerSupply = sender.aircraft.GetPowerSupply();
		if (powerSupply != null)
		{
			powerSupply.onChargeChanged += ChargeIndicator_OnChargeChanged;
		}
		SetVisibility(visible: false);
	}

	private void SetVisibility(bool visible)
	{
		if (visible)
		{
			base.enabled = true;
			base.gameObject.SetActive(value: true);
			UpdateDisplay();
		}
		else
		{
			base.enabled = false;
			base.gameObject.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		CombatHUD.onSetAircraft -= ChargeIndicator_OnSetAircraft;
	}

	private void ChargeIndicator_OnChargeChanged(object sender)
	{
		if (powerSupply.Users > 0 && !base.enabled)
		{
			SetVisibility(visible: true);
		}
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		float powerSupplied = powerSupply.GetPowerSupplied();
		float charge = powerSupply.GetCharge();
		Color color = barGradient.Evaluate(powerSupply.GetPowerAvailable());
		Color color2 = color;
		color.a = Mathf.Lerp(0.2f, color.a, Mathf.Sqrt(powerSupplied));
		chargeBar.rectTransform.sizeDelta = new Vector2(charge * barLength, barHeight);
		chargeBar.color = color;
		readout.text = $"CAPACITOR {powerSupply.GetChargeKJ():0}  kJ";
		readout.color = color2;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
