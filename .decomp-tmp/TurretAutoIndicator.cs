using UnityEngine;
using UnityEngine.UI;

public class TurretAutoIndicator : HUDApp
{
	[SerializeField]
	private Image safetyIcon;

	public override void Initialize(Aircraft aircraft)
	{
		CombatHUD.onSetTurretAuto += TurretAutoIndicator_OnToggleAuto;
		safetyIcon.enabled = !SceneSingleton<CombatHUD>.i.turretAutoControl;
	}

	public new void RefreshSettings()
	{
	}

	private void OnDestroy()
	{
		CombatHUD.onSetTurretAuto -= TurretAutoIndicator_OnToggleAuto;
	}

	private void TurretAutoIndicator_OnToggleAuto()
	{
		safetyIcon.enabled = !SceneSingleton<CombatHUD>.i.turretAutoControl;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
