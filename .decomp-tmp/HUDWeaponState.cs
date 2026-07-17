using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDWeaponState : MonoBehaviour
{
	protected WeaponInfo weaponInfo;

	protected List<Unit> targetList;

	public virtual void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
	}

	public virtual void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		weaponInfo = weaponStation.WeaponInfo;
	}

	public virtual void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
