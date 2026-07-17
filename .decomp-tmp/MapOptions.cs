public class MapOptions : SceneSingleton<MapOptions>
{
	public enum TooltipType
	{
		None,
		Info,
		Ammo,
		Order
	}

	public MFDScreen screen;

	public TooltipType tooltipType = TooltipType.Info;

	public bool showObjectives = true;

	public bool showTargetInfo = true;

	public bool showJamming = true;

	public bool showPilotIcons = true;

	public bool showGridLabels = true;

	public bool showAirbaseIcon = true;

	public float iconSize = 1f;

	public void ToggleShowObjectives()
	{
		showObjectives = !showObjectives;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowTargetInfo()
	{
		showTargetInfo = !showTargetInfo;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowJamming()
	{
		showJamming = !showJamming;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowGridLabels()
	{
		showGridLabels = !showGridLabels;
	}

	public void SetToolTipType(int value)
	{
		tooltipType = (TooltipType)value;
	}

	public void SetIconSize(int value)
	{
		iconSize = 0.6f + 0.2f * (float)value;
	}

	public void ToggleShowPilotIcons()
	{
		showPilotIcons = !showPilotIcons;
		foreach (MapIcon mapIcon in SceneSingleton<DynamicMap>.i.mapIcons)
		{
			if (mapIcon is UnitMapIcon unitMapIcon && unitMapIcon.unit is PilotDismounted)
			{
				mapIcon.gameObject.SetActive(showPilotIcons);
			}
		}
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowAirbaseIcons()
	{
		showAirbaseIcon = !showAirbaseIcon;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
