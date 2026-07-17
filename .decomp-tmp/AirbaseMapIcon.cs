using UnityEngine;

public class AirbaseMapIcon : MapIcon
{
	public Airbase airbase { get; private set; }

	protected override FactionHQ GetHQ()
	{
		return airbase.CurrentHQ;
	}

	protected override bool IsLocalPlayerAircraft()
	{
		return false;
	}

	protected override void OnSelectIcon()
	{
	}

	protected override void OnDeselectIcon()
	{
	}

	protected override void OnRemoveIcon()
	{
	}

	public void SetIcon(Airbase airbase)
	{
		base.transform.localScale = Vector3.one;
		base.transform.eulerAngles = Vector3.zero;
		iconImage.sprite = GameAssets.i.airbaseSprite;
		this.airbase = airbase;
		iconImage.transform.localScale = 50f * Vector3.one;
		UpdateColor();
	}

	protected override Color GetColor()
	{
		if (!airbase.AnyHangarsAvailable())
		{
			return GameAssets.i.HUDAirbaseNotAvailable;
		}
		if (isSelected)
		{
			return GameAssets.i.HUDFriendlySelected;
		}
		return GameAssets.i.HUDFriendly;
	}

	public override void ClickIcon(ClickSource clickSource)
	{
		if ((!(SceneSingleton<CombatHUD>.i.aircraft != null) || SceneSingleton<CombatHUD>.i.aircraft.disabled) && GameManager.gameResolution != GameResolution.Defeat && airbase.AnyHangarsAvailable())
		{
			SceneSingleton<DynamicMap>.i.UnselectAll();
			SceneSingleton<DynamicMap>.i.DeselectAllIcons();
			SceneSingleton<DynamicMap>.i.SelectIcon(airbase);
			SceneSingleton<GameplayUI>.i.SelectAirbase(airbase);
			iconImage.raycastTarget = false;
		}
	}

	public override void UpdateIcon(float mapDisplayFactor, float mapInverseScale, Transform mapTransform, bool mapMaximized)
	{
		if (!(airbase == null))
		{
			UpdateColor();
			base.gameObject.SetActive(mapMaximized && SceneSingleton<MapOptions>.i.showAirbaseIcon);
			globalPosition = airbase.center.GlobalPosition().AsVector3() * mapDisplayFactor;
			iconImage.transform.localPosition = new Vector3(globalPosition.x, globalPosition.z, 0f);
			iconImage.transform.eulerAngles = Vector3.zero;
			iconImage.transform.localScale = mapInverseScale * 50f * Vector3.one;
		}
	}

	public override string GetInfoText()
	{
		if (airbase.disabled)
		{
			return airbase.SavedAirbase.DisplayName + "\n(Disabled)";
		}
		if (airbase.AnyHangarsAvailable())
		{
			return airbase.SavedAirbase.DisplayName;
		}
		return airbase.SavedAirbase.DisplayName + "\n(No Hangars)";
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
