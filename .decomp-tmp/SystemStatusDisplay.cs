using UnityEngine;
using UnityEngine.UI;

public class SystemStatusDisplay : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private Image systemEngineImg;

	[SerializeField]
	private Image systemFlightControlImg;

	[SerializeField]
	private Image systemRadarImg;

	[SerializeField]
	private Image systemGearImg;

	[SerializeField]
	private Image systemModeImg;

	[SerializeField]
	private Image systemFuelImg;

	[SerializeField]
	private Text systemEngineTxt;

	[SerializeField]
	private Text systemFlightControlTxt;

	[SerializeField]
	private Text systemRadarTxt;

	[SerializeField]
	private Text systemGearTxt;

	[SerializeField]
	private Text systemModeTxt;

	[SerializeField]
	private Text systemFuelTxt;

	public override void Initialize(Aircraft aircraft)
	{
		if (aircraft == null)
		{
			return;
		}
		this.aircraft = aircraft;
		aircraft.onSetGear += OnSetGear;
		if (aircraft.gearDeployed)
		{
			systemGearImg.color = Color.green;
			systemGearTxt.color = Color.green;
		}
		else
		{
			systemGearImg.color = Color.grey;
			systemGearTxt.color = Color.grey;
		}
		aircraft.onSetFlightAssist += OnSetFlightAssist;
		if (aircraft.radar != null)
		{
			systemRadarTxt.text = "RADAR";
			OnSetRadar();
		}
		else
		{
			systemRadarTxt.text = "OPTICAL";
		}
		OnSetFuel();
		SceneSingleton<HUDOptions>.i.OnApplyOptions += OnHUDMode;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable += OnEngineDisable;
				component.OnEngineDamage += OnEngineDamage;
			}
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		systemEngineTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		systemFlightControlTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		systemRadarTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		systemGearTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		systemModeTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		systemFuelTxt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			if (aircraft.radar != null)
			{
				OnSetRadar();
			}
			OnSetFuel();
			OnHUDMode();
		}
	}

	public void OnSetGear(Aircraft.OnSetGear onSetGear)
	{
		if (onSetGear.gearState == LandingGear.GearState.LockedRetracted)
		{
			systemGearImg.color = Color.grey;
			systemGearTxt.color = Color.grey;
		}
		else if (onSetGear.gearState == LandingGear.GearState.Extending || onSetGear.gearState == LandingGear.GearState.Retracting)
		{
			systemGearImg.color = Color.yellow;
			systemGearTxt.color = Color.yellow;
		}
		else
		{
			systemGearImg.color = Color.green;
			systemGearTxt.color = Color.green;
		}
	}

	public void OnSetFlightAssist(Aircraft.OnFlightAssistToggle onFlightAssistToggle)
	{
		if (onFlightAssistToggle.enabled)
		{
			systemFlightControlImg.color = Color.green;
			systemFlightControlTxt.color = Color.green;
		}
		else
		{
			systemFlightControlImg.color = Color.grey;
			systemFlightControlTxt.color = Color.grey;
		}
	}

	public void OnSetRadar()
	{
		if (aircraft.radar.activated)
		{
			systemRadarImg.color = Color.green;
			systemRadarTxt.color = Color.green;
		}
		else
		{
			systemRadarImg.color = Color.grey;
			systemRadarTxt.color = Color.grey;
		}
	}

	public void OnSetFuel()
	{
		if (aircraft.GetFuelLevel() < 0.1f)
		{
			systemFuelImg.color = Color.red;
			systemFuelTxt.color = Color.red;
		}
		else if (aircraft.GetFuelLevel() < 0.2f)
		{
			systemFuelImg.color = new Color(1f, 0.5f, 0f);
			systemFuelTxt.color = new Color(1f, 0.5f, 0f);
		}
		else if (aircraft.GetFuelLevel() < 0.5f)
		{
			systemFuelImg.color = Color.yellow;
			systemFuelTxt.color = Color.yellow;
		}
		else
		{
			systemFuelImg.color = Color.green;
			systemFuelTxt.color = Color.green;
		}
	}

	public void OnHUDMode()
	{
		systemModeTxt.text = $"MODE : {SceneSingleton<HUDOptions>.i.currentMode}";
		if (SceneSingleton<HUDOptions>.i.currentMode == HUDOptions.HUDMode.NAV)
		{
			systemModeImg.color = Color.white;
			systemModeTxt.color = Color.white;
		}
		else
		{
			systemModeImg.color = Color.green;
			systemModeTxt.color = Color.green;
		}
	}

	public void OnEngineDamage()
	{
		systemEngineImg.color = Color.yellow;
		systemEngineTxt.color = Color.yellow;
	}

	public void OnEngineDisable()
	{
		systemEngineImg.color = Color.red;
		systemEngineTxt.color = Color.red;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable -= OnEngineDisable;
				component.OnEngineDamage -= OnEngineDamage;
			}
		}
	}

	private void OnDestroy()
	{
		if (SceneSingleton<HUDOptions>.i != null)
		{
			SceneSingleton<HUDOptions>.i.OnApplyOptions -= OnHUDMode;
		}
		if (!(aircraft != null))
		{
			return;
		}
		aircraft.onSetGear -= OnSetGear;
		aircraft.onSetFlightAssist -= OnSetFlightAssist;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable -= OnEngineDisable;
				component.OnEngineDamage -= OnEngineDamage;
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
