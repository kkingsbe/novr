using UnityEngine;

public class MFDAppManager : SceneSingleton<MFDAppManager>
{
	private Aircraft aircraft;

	[SerializeField]
	private HUDApp[] apps;

	private void Start()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		aircraft.onDisableUnit += HUDAppManager_OnUnitDisable;
		PlayerSettings.OnApplyOptions += RefreshSettings;
		HUDApp[] array = apps;
		foreach (HUDApp obj in array)
		{
			obj.Initialize(aircraft);
			obj.RefreshSettings();
		}
	}

	private void Update()
	{
		for (int i = 0; i < apps.Length; i++)
		{
			apps[i].Refresh();
		}
	}

	public void RefreshSettings()
	{
		HUDApp[] array = apps;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RefreshSettings();
		}
	}

	private void HUDAppManager_OnUnitDisable(Unit unit)
	{
		aircraft.onDisableUnit -= HUDAppManager_OnUnitDisable;
		PlayerSettings.OnApplyOptions -= RefreshSettings;
		Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
