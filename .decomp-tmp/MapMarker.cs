using UnityEngine;
using UnityEngine.UI;

public abstract class MapMarker : MonoBehaviour
{
	[SerializeField]
	public Image markerImg;

	public Color color = Color.white;

	public float lastRefresh;

	public float refreshDelay = 1f;

	public virtual void Remove()
	{
		if (this != null)
		{
			SceneSingleton<DynamicMap>.i.mapMarkers.Remove(this);
			Object.Destroy(base.gameObject);
		}
	}

	public virtual void DynamicHide()
	{
	}

	public virtual void Mask()
	{
	}

	public virtual void Show(bool value)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
