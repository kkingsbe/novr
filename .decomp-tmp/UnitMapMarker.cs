public abstract class UnitMapMarker : MapMarker
{
	public UnitMapIcon Icon { get; private set; }

	public Unit GetUnit()
	{
		if (!(Icon != null))
		{
			return null;
		}
		return Icon.unit;
	}

	protected abstract void ExtraSetup();

	public void Setup(UnitMapIcon icon)
	{
		Icon = icon;
		ExtraSetup();
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
