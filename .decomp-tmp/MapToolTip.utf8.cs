using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MapToolTip : MonoBehaviour
{
	[SerializeField]
	private Image line1;

	[SerializeField]
	private Image line2;

	[SerializeField]
	private Image circle1;

	[SerializeField]
	private Image circle2;

	[SerializeField]
	private Text infoText;

	[SerializeField]
	private GameObject iconContainer;

	[SerializeField]
	private Sprite circleThin;

	[SerializeField]
	private Sprite circleThick;

	[SerializeField]
	private List<TooltipItem> listToolTips = new List<TooltipItem>();

	private MapIcon icon;

	private float lastRefresh;

	private float refreshRate = 0.2f;

	private List<WeaponInfo> listWeapons = new List<WeaponInfo>();

	private void OnDestroy()
	{
		DynamicMap.onMapChanged -= DynamicMap_onMapChange;
	}

	private void Start()
	{
		DynamicMap.onMapChanged += DynamicMap_onMapChange;
		ResetAll();
		Refresh(icon);
	}

	private void DynamicMap_onMapChange()
	{
		Refresh(icon);
	}

	public void RefreshAirbaseTooltip(Airbase airbase)
	{
		TooltipItem tooltipItem = listToolTips[0];
		TooltipItem tooltipItem2 = listToolTips[1];
		TooltipItem tooltipItem3 = listToolTips[2];
		TooltipItem tooltipItem4 = listToolTips[3];
		TooltipItem tooltipItem5 = listToolTips[4];
		tooltipItem.gameObject.SetActive(value: true);
		tooltipItem2.gameObject.SetActive(value: true);
		tooltipItem3.gameObject.SetActive(value: true);
		tooltipItem4.gameObject.SetActive(value: true);
		tooltipItem5.gameObject.SetActive(value: true);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int warheads = airbase.GetWarheads();
		if (airbase.AnyHangarsAvailable())
		{
			for (int i = 0; i < airbase.hangars.Count; i++)
			{
				if (!airbase.hangars[i].Disabled)
				{
					if (airbase.hangars[i].attachedUnit.definition.code == "HPAD")
					{
						num++;
					}
					else if (airbase.hangars[i].attachedUnit.definition.code == "REV")
					{
						num2++;
					}
					else if (airbase.hangars[i].attachedUnit.definition.code == "HGR-M")
					{
						num3++;
					}
					else if (airbase.hangars[i].attachedUnit.definition.code == "HGR-H")
					{
						num4++;
					}
				}
			}
			circle1.transform.localScale = Vector3.one;
			circle1.rectTransform.sizeDelta = 70f * Vector2.one;
			circle1.enabled = true;
			circle1.color = GameAssets.i.HUDFriendly;
			circle1.fillAmount = airbase.capture.controlBalance;
			circle2.transform.localScale = Vector3.one;
			circle2.rectTransform.sizeDelta = 70f * Vector2.one;
			circle2.enabled = true;
			circle2.color = GameAssets.i.HUDNeutral;
			circle2.fillClockwise = false;
			circle2.fillAmount = 1f - circle1.fillAmount;
		}
		tooltipItem.Setup(GameAssets.i.hangarSprite_helipad, (num > 0) ? Color.white : Color.grey, null, num);
		tooltipItem2.Setup(GameAssets.i.hangarSprite_revetment, (num2 > 0) ? Color.white : Color.grey, null, num2);
		tooltipItem3.Setup(GameAssets.i.hangarSprite_medium, (num3 > 0) ? Color.white : Color.grey, null, num3);
		tooltipItem4.Setup(GameAssets.i.hangarSprite_shelter, (num4 > 0) ? Color.white : Color.grey, null, num4);
		tooltipItem5.Setup(GameAssets.i.warheadSprite, (warheads > 0) ? Color.white : Color.grey, null, warheads);
	}

	public void RefreshFactoryTooltip(Building building, Factory factory)
	{
		if (building.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null)
		{
			TooltipItem tooltipItem = listToolTips[0];
			tooltipItem.gameObject.SetActive(value: true);
			if (factory.GetProduction() != "")
			{
				tooltipItem.Setup(null, Color.white, $"{factory.GetProduction()}\n{factory.GetNextProduction(absolute: true)}", null);
			}
			else
			{
				tooltipItem.gameObject.SetActive(value: false);
			}
			circle1.transform.localScale = Vector3.one;
			circle1.rectTransform.sizeDelta = 50f * Vector2.one;
			circle1.enabled = tooltipItem.gameObject.activeSelf;
			circle1.fillAmount = 1f - factory.GetNextProduction(absolute: false);
			circle1.color = Color.green;
			circle2.transform.localScale = Vector3.one;
			circle2.rectTransform.sizeDelta = 50f * Vector2.one;
			circle2.enabled = tooltipItem.gameObject.activeSelf;
			circle2.fillClockwise = false;
			circle2.fillAmount = factory.GetNextProduction(absolute: false);
			circle2.color = Color.grey;
		}
	}

	public void RefreshCarrierTooltip(Airbase airbase)
	{
		TooltipItem tooltipItem = listToolTips[0];
		TooltipItem tooltipItem2 = listToolTips[4];
		tooltipItem.gameObject.SetActive(value: true);
		tooltipItem2.gameObject.SetActive(value: true);
		int num = 0;
		int warheads = airbase.GetWarheads();
		if (airbase.AnyHangarsAvailable())
		{
			for (int i = 0; i < airbase.hangars.Count; i++)
			{
				if (!airbase.hangars[i].Disabled && airbase.hangars[i].attachedUnit.definition.code == "SHP")
				{
					num++;
				}
			}
		}
		tooltipItem.Setup(GameAssets.i.hangarSprite_carrier, (num > 0) ? Color.white : Color.grey, null, num);
		tooltipItem2.Setup(GameAssets.i.warheadSprite, (warheads > 0) ? Color.white : Color.grey, null, warheads);
	}

	public void RefreshAirUnitTooltip(Unit unit)
	{
		TooltipItem tooltipItem = listToolTips[0];
		TooltipItem tooltipItem2 = listToolTips[1];
		TooltipItem tooltipItem3 = listToolTips[2];
		TooltipItem tooltipItem4 = listToolTips[0];
		if (!SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker2 && targetMarker2.Icon != null && targetMarker2.Icon.unit == unit) && !tooltipItem.gameObject.activeSelf)
		{
			if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
			{
				tooltipItem4.gameObject.SetActive(value: true);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
			{
				tooltipItem.gameObject.SetActive(value: true);
				tooltipItem2.gameObject.SetActive(value: true);
				tooltipItem3.gameObject.SetActive(value: true);
			}
		}
		else if ((bool)SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker && targetMarker.Icon != null && targetMarker.Icon.unit == unit) && tooltipItem.gameObject.activeSelf)
		{
			if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
			{
				tooltipItem4.gameObject.SetActive(value: false);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
			{
				tooltipItem.gameObject.SetActive(value: false);
				tooltipItem2.gameObject.SetActive(value: false);
				tooltipItem3.gameObject.SetActive(value: false);
			}
		}
		if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null || SceneSingleton<DynamicMap>.i.HQ.IsTargetPositionAccurate(unit, 20f))
			{
				tooltipItem.Setup(null, Color.white, "SPD " + UnitConverter.SpeedReading(unit.speed), null);
				tooltipItem2.Setup(null, Color.white, "ALT " + UnitConverter.AltitudeReading(unit.radarAlt), null);
				tooltipItem3.Setup(null, Color.white, $"HDG {Mathf.RoundToInt(unit.transform.eulerAngles.y)}ø", null);
			}
			else
			{
				tooltipItem.Setup(null, Color.white, "SPD -", null);
				tooltipItem2.Setup(null, Color.white, "ALT -", null);
				tooltipItem3.Setup(null, Color.white, "HDG -", null);
			}
		}
		else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null)
			{
				string text = "";
				for (int i = 0; i < listWeapons.Count; i++)
				{
					text = text + listWeapons[i].weaponName + " ";
					int num = 0;
					int num2 = 0;
					for (int j = 0; j < unit.weaponStations.Count; j++)
					{
						if (unit.weaponStations[j].WeaponInfo == listWeapons[i])
						{
							num += unit.weaponStations[j].Ammo;
							num2 += unit.weaponStations[j].FullAmmo;
						}
					}
					text += $"{num}/{num2} ";
					text += "\n";
				}
				tooltipItem4.Setup(null, Color.white, text, null);
			}
			else
			{
				tooltipItem4.Setup(null, Color.white, "", null);
			}
		}
		float num3 = 0f;
		float num4 = 0f;
		Aircraft aircraft = unit as Aircraft;
		if (aircraft.weaponManager != null && aircraft.weaponManager.currentWeaponStation?.WeaponInfo != null)
		{
			num3 = aircraft.weaponManager.currentWeaponStation.WeaponInfo.targetRequirements.maxRange;
		}
		TargetDetector[] componentsInChildren = unit.transform.GetComponentsInChildren<TargetDetector>();
		if (componentsInChildren.Length != 0)
		{
			for (int k = 0; k < componentsInChildren.Length; k++)
			{
				if (componentsInChildren[k] is Radar && componentsInChildren[k].GetRadarRange() > num4)
				{
					num4 = componentsInChildren[k].GetRadarRange();
				}
				else if (componentsInChildren[k].GetVisualRange() > num4)
				{
					num4 = componentsInChildren[k].GetVisualRange();
				}
			}
		}
		Vector3 vector = 2f * num3 * SceneSingleton<DynamicMap>.i.MetersToPixels() * Vector3.one;
		Vector3 vector2 = 2f * num4 * SceneSingleton<DynamicMap>.i.MetersToPixels() * Vector3.one;
		circle1.rectTransform.sizeDelta = Vector2.one;
		circle1.sprite = circleThin;
		circle1.color = Color.red;
		circle1.transform.localScale = vector * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		circle2.rectTransform.sizeDelta = Vector2.one;
		circle2.sprite = circleThin;
		circle2.color = Color.cyan;
		circle2.transform.localScale = vector2 * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		List<Unit> targetList = aircraft.weaponManager.GetTargetList();
		if (targetList != null && targetList.Count > 0 && targetList[0] != null)
		{
			unit.NetworkHQ.TryGetKnownPosition(targetList[0], out var knownPosition);
			Vector3 vector3 = FastMath.Direction(unit.GlobalPosition(), knownPosition);
			Vector3 to = new Vector3(vector3.x, 0f, vector3.z);
			float num5 = 0f;
			float num6 = Vector3.SignedAngle(Vector3.forward, to, Vector3.up);
			if (DynamicMap.TryGetMapIcon(targetList[0], out var mapIcon))
			{
				num5 = Vector3.Distance(mapIcon.transform.localPosition, icon.transform.localPosition);
			}
			line1.rectTransform.sizeDelta = new Vector2(1f, num5 * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x);
			line1.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f - num6);
			Color color = Color.grey;
			if (aircraft.weaponManager.currentWeaponStation.WeaponInfo.jammer)
			{
				color = Color.yellow;
			}
			else if (aircraft.weaponManager.currentWeaponStation.WeaponInfo.effectiveness.antiSurface > 0f)
			{
				color = Color.red;
			}
			else if (aircraft.weaponManager.currentWeaponStation.WeaponInfo.effectiveness.antiAir > 0f)
			{
				color = Color.cyan;
			}
			color.a = 0.6f;
			line1.color = color;
		}
		line1.enabled = SceneSingleton<DynamicMap>.i.HQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ;
		circle1.enabled = SceneSingleton<DynamicMap>.i.HQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ;
		circle2.enabled = SceneSingleton<DynamicMap>.i.HQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ;
	}

	public void RefreshMissileUnitTooltip(Unit unit)
	{
		TooltipItem tooltipItem = listToolTips[0];
		TooltipItem tooltipItem2 = listToolTips[1];
		TooltipItem tooltipItem3 = listToolTips[2];
		if (!SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker2 && targetMarker2.Icon != null && targetMarker2.Icon.unit == unit) && !tooltipItem.gameObject.activeSelf)
		{
			tooltipItem.gameObject.SetActive(value: true);
			tooltipItem2.gameObject.SetActive(value: true);
			tooltipItem3.gameObject.SetActive(value: true);
		}
		else if ((bool)SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker && targetMarker.Icon != null && targetMarker.Icon.unit == unit) && tooltipItem.gameObject.activeSelf)
		{
			tooltipItem.gameObject.SetActive(value: false);
			tooltipItem2.gameObject.SetActive(value: false);
			tooltipItem3.gameObject.SetActive(value: false);
		}
		if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null || SceneSingleton<DynamicMap>.i.HQ.IsTargetPositionAccurate(unit, 20f))
		{
			tooltipItem.Setup(null, Color.white, "SPD " + UnitConverter.SpeedReading(unit.speed), null);
			tooltipItem2.Setup(null, Color.white, "ALT " + UnitConverter.AltitudeReading(unit.GlobalPosition().y), null);
			tooltipItem3.Setup(null, Color.white, $"HDG {Mathf.RoundToInt(unit.transform.eulerAngles.y)}ø", null);
		}
		else
		{
			tooltipItem.Setup(null, Color.white, "SPD -", null);
			tooltipItem2.Setup(null, Color.white, "ALT -", null);
			tooltipItem3.Setup(null, Color.white, "HDG -", null);
		}
		UnitRegistry.TryGetUnit((unit as Missile).targetID, out var unit2);
		if (unit2 != null)
		{
			unit.NetworkHQ.TryGetKnownPosition(unit2, out var knownPosition);
			Vector3 vector = FastMath.Direction(unit.GlobalPosition(), knownPosition);
			Vector3 to = new Vector3(vector.x, 0f, vector.z);
			float num = 0f;
			float num2 = Vector3.SignedAngle(Vector3.forward, to, Vector3.up);
			if (DynamicMap.TryGetMapIcon(unit2, out var mapIcon))
			{
				num = Vector3.Distance(mapIcon.transform.localPosition, icon.transform.localPosition);
			}
			line1.rectTransform.sizeDelta = new Vector2(1f, num * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x);
			line1.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f - num2);
			Color red = Color.red;
			red.a = 0.6f;
			line1.color = red;
		}
		line1.enabled = SceneSingleton<DynamicMap>.i.HQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ;
	}

	public void RefreshGroundUnitTooltip(Unit unit)
	{
		TooltipItem tooltipItem = listToolTips[0];
		_ = listToolTips[1];
		TooltipItem tooltipItem2 = listToolTips[2];
		TooltipItem tooltipItem3 = listToolTips[0];
		TooltipItem orderPlayer = listToolTips[0];
		TooltipItem orderTime = listToolTips[1];
		if (!SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker2 && targetMarker2.Icon != null && targetMarker2.Icon.unit == unit) && !listToolTips[0].gameObject.activeSelf)
		{
			if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
			{
				tooltipItem3.gameObject.SetActive(value: true);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
			{
				tooltipItem.gameObject.SetActive(value: true);
				tooltipItem2.gameObject.SetActive(value: true);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Order)
			{
				orderPlayer.gameObject.SetActive(value: true);
				orderTime.gameObject.SetActive(value: true);
			}
		}
		else if ((bool)SceneSingleton<DynamicMap>.i.mapMarkers.Find((MapMarker x) => x is TargetMarker targetMarker && targetMarker.Icon != null && targetMarker.Icon.unit == unit) && listToolTips[0].gameObject.activeSelf)
		{
			if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
			{
				tooltipItem3.gameObject.SetActive(value: false);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
			{
				tooltipItem.gameObject.SetActive(value: false);
				tooltipItem2.gameObject.SetActive(value: false);
			}
			else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Order)
			{
				orderPlayer.gameObject.SetActive(value: false);
				orderTime.gameObject.SetActive(value: false);
			}
		}
		if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Info)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null || SceneSingleton<DynamicMap>.i.HQ.IsTargetPositionAccurate(unit, 20f))
			{
				tooltipItem.Setup(null, Color.white, "SPD " + UnitConverter.SpeedReading(unit.speed), null);
				tooltipItem2.Setup(null, Color.white, $"HDG {Mathf.RoundToInt(unit.transform.eulerAngles.y)}ø", null);
			}
			else
			{
				tooltipItem.Setup(null, Color.white, "SPD -", null);
				tooltipItem2.Setup(null, Color.white, "HDG -", null);
			}
		}
		else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null)
			{
				string text = "";
				for (int i = 0; i < listWeapons.Count; i++)
				{
					text = text + listWeapons[i].weaponName + " ";
					int num = 0;
					int num2 = 0;
					for (int j = 0; j < unit.weaponStations.Count; j++)
					{
						if (unit.weaponStations[j].WeaponInfo == listWeapons[i])
						{
							num += unit.weaponStations[j].Ammo;
							num2 += unit.weaponStations[j].FullAmmo;
						}
					}
					text += $"{num}/{num2} ";
					text += "\n";
				}
				tooltipItem3.Setup(null, Color.white, text, null);
			}
			else
			{
				tooltipItem3.Setup(null, Color.white, "", null);
			}
		}
		else if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Order)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null)
			{
				RefreshOrderFromServer(unit, delegate(UnitCommand.Command lastOrder)
				{
					string desc = UnitConverter.TimeOfDay(lastOrder.time / 3600f, includeSeconds: true);
					if (lastOrder.player != null)
					{
						orderPlayer.Setup(null, Color.white, lastOrder.player.PlayerName ?? "", null);
					}
					else
					{
						orderPlayer.Setup(null, Color.white, "AI", null);
					}
					orderTime.Setup(null, Color.white, desc, null);
					if (lastOrder.position.AsVector3().magnitude > 1f)
					{
						Vector3 vector3 = FastMath.Direction(unit.GlobalPosition(), lastOrder.position);
						float num5 = Vector3.SignedAngle(to: new Vector3(vector3.x, 0f, vector3.z), from: Vector3.forward, axis: Vector3.up);
						float num6 = Vector3.Distance(unit.GlobalPosition().AsVector3(), lastOrder.position.AsVector3()) * SceneSingleton<DynamicMap>.i.MetersToPixels();
						line2.rectTransform.sizeDelta = new Vector2(1f, num6 * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x);
						line2.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f - num5);
						Color yellow = Color.yellow;
						yellow.a = 0.6f;
						line2.color = yellow;
						line2.enabled = true;
					}
				});
			}
			else
			{
				orderPlayer.Setup(null, Color.white, "?", null);
				orderTime.Setup(null, Color.white, "?", null);
			}
		}
		float num3 = 0f;
		if (unit.weaponStations.Count > 0)
		{
			for (int k = 0; k < unit.weaponStations.Count; k++)
			{
				if (unit.weaponStations[k] != null && (unit.weaponStations[k].WeaponInfo.effectiveness.antiAir > 0f || unit.weaponStations[k].WeaponInfo.effectiveness.antiMissile > 0f) && unit.weaponStations[k].WeaponInfo.targetRequirements.maxRange > num3)
				{
					num3 = unit.weaponStations[k].WeaponInfo.targetRequirements.maxRange;
				}
			}
		}
		TargetDetector[] componentsInChildren = unit.transform.GetComponentsInChildren<TargetDetector>();
		float num4 = 0f;
		if (componentsInChildren.Length != 0)
		{
			for (int l = 0; l < componentsInChildren.Length; l++)
			{
				if (componentsInChildren[l] is Radar && componentsInChildren[l].GetRadarRange() > num4)
				{
					num4 = componentsInChildren[l].GetRadarRange();
				}
				else if (componentsInChildren[l].GetVisualRange() > num4)
				{
					num4 = componentsInChildren[l].GetVisualRange();
				}
			}
		}
		Vector3 vector = 2f * num3 * SceneSingleton<DynamicMap>.i.MetersToPixels() * Vector3.one;
		Vector3 vector2 = 2f * num4 * SceneSingleton<DynamicMap>.i.MetersToPixels() * Vector3.one;
		circle1.rectTransform.sizeDelta = Vector2.one;
		circle1.sprite = circleThin;
		circle1.color = Color.red;
		circle1.transform.localScale = vector * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		circle2.rectTransform.sizeDelta = Vector2.one;
		circle2.sprite = circleThin;
		circle2.color = Color.cyan;
		circle2.transform.localScale = vector2 * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		circle1.enabled = true;
		circle2.enabled = true;
	}

	private void RefreshOrderFromServer(Unit unit, Action<UnitCommand.Command> setUI)
	{
		if (!(unit is ICommandable commandable))
		{
			setUI(default(UnitCommand.Command));
			return;
		}
		UnitCommand unitCommand = commandable.UnitCommand;
		setUI(unitCommand.GetCommandCached());
		if (unitCommand.IsServer)
		{
			return;
		}
		UniTask.Void(async delegate
		{
			MapIcon currentIcon = icon;
			UnitCommand.Command obj = await unitCommand.CmdTryGetCommand();
			if (currentIcon == icon)
			{
				setUI(obj);
			}
		});
	}

	public void ShowTooltip(MapIcon mapIcon)
	{
		icon = mapIcon;
		if (icon != null)
		{
			base.gameObject.SetActive(value: true);
			ResetAll();
			Refresh(icon);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void Refresh(MapIcon icon)
	{
		if (Time.unscaledTime < lastRefresh + refreshRate || icon == null)
		{
			return;
		}
		lastRefresh = Time.unscaledTime;
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		base.transform.localScale = Vector3.one * num;
		base.transform.position = icon.transform.position;
		infoText.text = icon.GetInfoText();
		infoText.color = icon.iconImage.color;
		if (SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.None || GameManager.gameState == GameState.Editor)
		{
			return;
		}
		if (icon is AirbaseMapIcon airbaseMapIcon)
		{
			airbaseMapIcon.airbase.TryGetAttachedUnit(out var attachedUnit);
			if (attachedUnit != null)
			{
				RefreshCarrierTooltip(airbaseMapIcon.airbase);
			}
			else
			{
				RefreshAirbaseTooltip(airbaseMapIcon.airbase);
			}
		}
		else
		{
			if (!(icon is UnitMapIcon unitMapIcon))
			{
				return;
			}
			foreach (WeaponStation weaponStation in unitMapIcon.unit.weaponStations)
			{
				if (!listWeapons.Contains(weaponStation.WeaponInfo))
				{
					listWeapons.Add(weaponStation.WeaponInfo);
				}
			}
			listWeapons.Sort((WeaponInfo a, WeaponInfo b) => a.costPerRound.CompareTo(b.costPerRound));
			if (unitMapIcon.unit is Aircraft)
			{
				RefreshAirUnitTooltip(unitMapIcon.unit);
			}
			else if (unitMapIcon.unit is Missile)
			{
				RefreshMissileUnitTooltip(unitMapIcon.unit);
			}
			else if (unitMapIcon.Factory != null)
			{
				RefreshFactoryTooltip((Building)unitMapIcon.unit, unitMapIcon.Factory);
			}
			else
			{
				RefreshGroundUnitTooltip(unitMapIcon.unit);
			}
		}
	}

	public void ResetAll()
	{
		foreach (TooltipItem listToolTip in listToolTips)
		{
			listToolTip.gameObject.SetActive(value: false);
		}
		infoText.text = "";
		circle1.transform.localScale = Vector3.zero;
		circle1.rectTransform.sizeDelta = Vector2.zero;
		circle1.fillClockwise = true;
		circle1.fillAmount = 1f;
		circle1.color = Color.white;
		circle1.sprite = circleThick;
		circle1.enabled = false;
		circle2.transform.localScale = Vector3.zero;
		circle2.rectTransform.sizeDelta = Vector2.zero;
		circle2.sprite = circleThick;
		circle2.fillAmount = 1f;
		circle2.color = Color.white;
		circle2.fillClockwise = true;
		circle2.enabled = false;
		line1.enabled = false;
		line1.rectTransform.sizeDelta = Vector2.zero;
		line2.enabled = false;
		line2.rectTransform.sizeDelta = Vector2.zero;
		listWeapons.Clear();
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
