using UnityEngine;

public class HUDApp : MonoBehaviour
{
	protected enum AppType
	{
		HUD,
		HMD,
		MFD
	}

	[SerializeField]
	protected AppType type;

	[SerializeField]
	protected float fontSizeMultiplier = 1f;

	protected int fontSize;

	protected Color fontColor;

	public virtual void Initialize(Aircraft aircraft)
	{
	}

	public virtual void Refresh()
	{
	}

	public virtual void RefreshSettings()
	{
		switch (type)
		{
		case AppType.HUD:
			fontSize = (int)PlayerSettings.hudTextSize;
			fontColor = new Color(PlayerSettings.hudColorR / 255, PlayerSettings.hudColorG / 255, PlayerSettings.hudColorB / 255);
			break;
		case AppType.HMD:
			fontSize = (int)PlayerSettings.hmdTextSize;
			fontColor = new Color(PlayerSettings.hudColorR / 255, PlayerSettings.hudColorG / 255, PlayerSettings.hudColorB / 255);
			break;
		case AppType.MFD:
			fontSize = (int)PlayerSettings.hudTextSize;
			break;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
