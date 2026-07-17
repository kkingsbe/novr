using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDNoWeaponState : HUDWeaponState
{
	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		SceneSingleton<FlightHud>.i.velocityVector.color = new Color(0f, 1f, 0f, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.color = Color.Lerp(Color.black, Color.green, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.GetHUDCenter().position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = !aircraft.gearDeployed;
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		targetDesignator.transform.localScale = Vector3.one;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
