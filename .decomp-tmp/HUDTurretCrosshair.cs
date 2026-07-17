using UnityEngine;
using UnityEngine.UI;

public class HUDTurretCrosshair : MonoBehaviour
{
	[SerializeField]
	private Image circle;

	[SerializeField]
	private Image readinessCircle;

	[SerializeField]
	private Image crosshair;

	private Turret turret;

	private Gun gun;

	public void Initialize(Turret turret)
	{
		this.turret = turret;
		if (turret.GetWeapon() is Gun gun)
		{
			this.gun = gun;
		}
	}

	public void Refresh(Camera mainCamera, out Vector3 crosshairPosition)
	{
		Vector3 direction = turret.GetDirection();
		bool flag = turret.IsOnTarget();
		crosshairPosition = Vector3.one * 10000f;
		if (Vector3.Dot(mainCamera.transform.forward, direction - mainCamera.transform.position) > 0f)
		{
			crosshairPosition = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(direction);
			crosshairPosition.z = 0f;
			base.transform.position = crosshairPosition;
			crosshair.enabled = true;
			if (gun != null)
			{
				float reloadProgress = gun.GetReloadProgress();
				if (reloadProgress > 0f)
				{
					if (!readinessCircle.enabled)
					{
						readinessCircle.enabled = true;
						crosshair.color = Color.red + Color.green * 0.5f;
					}
					readinessCircle.fillAmount = reloadProgress;
				}
				else if (readinessCircle.enabled)
				{
					readinessCircle.enabled = false;
					crosshair.color = Color.green;
				}
				circle.enabled = flag && reloadProgress <= 0f;
			}
			else
			{
				circle.enabled = flag;
			}
		}
		else
		{
			circle.enabled = false;
			readinessCircle.enabled = false;
			crosshair.enabled = false;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
