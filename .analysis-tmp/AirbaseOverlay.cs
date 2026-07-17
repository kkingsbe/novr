using UnityEngine;
using UnityEngine.UI;

public class AirbaseOverlay : MonoBehaviour
{
	[SerializeField]
	private Image airbaseMarker;

	[SerializeField]
	private Text airbaseLabel;

	private Airbase nearestAirbase;

	private Airbase.Runway.RunwayUsage? runwayUsage;

	private bool landing;

	private bool taxiingToRunway;

	private bool reachedRunway;

	private string runwayName;

	private float takeoffTime;

	private Airbase.Runway landedAtRunway;

	[SerializeField]
	private Image[] runwayBorders;

	[SerializeField]
	private Image glideslope;

	[SerializeField]
	private Image glideslopeAimPoint;

	private Vector3[] runwayCorners;

	private GlobalPosition landingApproach;

	private void Awake()
	{
		glideslope.enabled = false;
		glideslopeAimPoint.enabled = false;
		Image[] array = runwayBorders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		runwayCorners = new Vector3[4];
	}

	private void OnEnable()
	{
		airbaseMarker.enabled = false;
		airbaseLabel.enabled = false;
		this.StartSlowUpdateDelayed(2f, UpdateNearestAirbase);
	}

	private void ShowRunwayBorders(bool show)
	{
		Image[] array = runwayBorders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = show;
		}
	}

	private void DrawRunwayBorders(Airbase.Runway runway)
	{
		Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
		float width = runway.GetWidth();
		Transform start = runway.Start;
		Transform end = runway.End;
		Vector3 vector = start.position - 0.5f * width * start.right;
		Vector3 vector2 = start.position + 0.5f * width * start.right;
		Vector3 vector3 = end.position + 0.5f * width * end.right;
		Vector3 vector4 = end.position - 0.5f * width * end.right;
		bool show = Vector3.Dot(mainCamera.transform.forward, vector - mainCamera.transform.position) > 0f && Vector3.Dot(mainCamera.transform.forward, vector2 - mainCamera.transform.position) > 0f && Vector3.Dot(mainCamera.transform.forward, vector3 - mainCamera.transform.position) > 0f && Vector3.Dot(mainCamera.transform.forward, vector4 - mainCamera.transform.position) > 0f;
		ShowRunwayBorders(show);
		float num = 1080f / (float)Screen.height;
		runwayCorners[0] = mainCamera.WorldToScreenPoint(vector);
		runwayCorners[1] = mainCamera.WorldToScreenPoint(vector2);
		runwayCorners[2] = mainCamera.WorldToScreenPoint(vector3);
		runwayCorners[3] = mainCamera.WorldToScreenPoint(vector4);
		runwayCorners[0].z = 0f;
		runwayCorners[1].z = 0f;
		runwayCorners[2].z = 0f;
		runwayCorners[3].z = 0f;
		runwayBorders[0].transform.position = runwayCorners[0];
		Vector3 vector5 = runwayCorners[1] - runwayCorners[0];
		float z = (0f - Mathf.Atan2(vector5.x, vector5.y)) * 57.29578f;
		runwayBorders[0].transform.eulerAngles = new Vector3(0f, 0f, z);
		runwayBorders[0].transform.localScale = Vector3.one + Vector3.up * vector5.magnitude * num;
		runwayBorders[1].transform.position = runwayCorners[1];
		vector5 = runwayCorners[2] - runwayCorners[1];
		z = (0f - Mathf.Atan2(vector5.x, vector5.y)) * 57.29578f;
		runwayBorders[1].transform.eulerAngles = new Vector3(0f, 0f, z);
		runwayBorders[1].transform.localScale = Vector3.one + Vector3.up * vector5.magnitude * num;
		runwayBorders[2].transform.position = runwayCorners[2];
		vector5 = runwayCorners[3] - runwayCorners[2];
		z = (0f - Mathf.Atan2(vector5.x, vector5.y)) * 57.29578f;
		runwayBorders[2].transform.eulerAngles = new Vector3(0f, 0f, z);
		runwayBorders[2].transform.localScale = Vector3.one + Vector3.up * vector5.magnitude * num;
		runwayBorders[3].transform.position = runwayCorners[3];
		vector5 = runwayCorners[0] - runwayCorners[3];
		z = (0f - Mathf.Atan2(vector5.x, vector5.y)) * 57.29578f;
		runwayBorders[3].transform.eulerAngles = new Vector3(0f, 0f, z);
		runwayBorders[3].transform.localScale = Vector3.one + Vector3.up * vector5.magnitude * num;
	}

	private bool DrawGlideslope(Aircraft aircraft, Airbase.Runway.RunwayUsage runwayUsage)
	{
		if (aircraft.radarAlt < 1f)
		{
			return false;
		}
		Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
		Vector3 position = runwayUsage.GetEnd().position;
		float num = FastMath.Distance(aircraft.transform.position, position);
		Vector3 velocity = runwayUsage.Runway.GetVelocity();
		float num2 = Vector3.Dot(aircraft.rb.velocity - velocity, (position - aircraft.transform.position).normalized);
		float num3 = num / num2;
		Vector3 glideslopeAimpoint = runwayUsage.Runway.GetGlideslopeAimpoint(aircraft, num * 0.9f, runwayUsage.Reverse, num3 * 0.9f);
		Vector3 vector = position;
		if (Vector3.Dot(glideslopeAimpoint - mainCamera.transform.position, mainCamera.transform.forward) < 0f || Vector3.Dot(vector - mainCamera.transform.position, mainCamera.transform.forward) < 0f)
		{
			return false;
		}
		Vector3 vector2 = mainCamera.WorldToScreenPoint(glideslopeAimpoint);
		Vector3 vector3 = mainCamera.WorldToScreenPoint(vector);
		vector2.z = 0f;
		vector3.z = 0f;
		glideslope.transform.position = vector3;
		Vector3 vector4 = vector2 - vector3;
		float z = (0f - Mathf.Atan2(vector4.x, vector4.y)) * 57.29578f;
		glideslope.transform.eulerAngles = new Vector3(0f, 0f, z);
		glideslope.transform.localScale = Vector3.one + Vector3.up * (vector4.magnitude * (1080f / (float)Screen.height) - 8f);
		glideslopeAimPoint.transform.position = vector2;
		return true;
	}

	private void UpdateNearestAirbase()
	{
		Aircraft aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		if (aircraft == null)
		{
			landing = false;
			taxiingToRunway = false;
			reachedRunway = false;
			takeoffTime = 0f;
			return;
		}
		if (landedAtRunway != null && !landedAtRunway.AircraftOnRunway(aircraft))
		{
			landedAtRunway.DeregisterLanding(aircraft);
			landedAtRunway = null;
		}
		AircraftParameters aircraftParameters = aircraft.GetAircraftParameters();
		float takeoffDistance = aircraftParameters.takeoffDistance;
		if (aircraft.radarAlt > 1f)
		{
			takeoffTime += 0.5f;
			if (takeoffTime >= 1f)
			{
				aircraft.pilots[0].flightInfo.HasTakenOff = true;
			}
		}
		RunwayQuery runwayQuery;
		if (!aircraft.pilots[0].flightInfo.HasTakenOff)
		{
			runwayQuery = default(RunwayQuery);
			runwayQuery.RunwayType = RunwayQueryType.Any;
			runwayQuery.MinSize = 10f;
			runwayQuery.LandingSpeed = 1f;
			if (aircraft.NetworkHQ.AnyNearAirbase(aircraft.transform.position, out nearestAirbase) && takeoffDistance > 0f && !taxiingToRunway)
			{
				runwayUsage = nearestAirbase.GetTakeoffRunway(aircraft, takeoffDistance);
				if (runwayUsage.HasValue)
				{
					runwayName = runwayUsage.Value.GetName();
					SceneSingleton<AircraftActionsReport>.i.ReportText("Cleared to taxi to " + runwayName, 10f);
					nearestAirbase.CmdRegisterUsage(aircraft, isUsing: true, null);
					taxiingToRunway = true;
				}
			}
			return;
		}
		float mass = aircraft.GetMass();
		float maxWeight = aircraft.definition.aircraftInfo.maxWeight;
		float landingSpeed = Mathf.Sqrt(mass / maxWeight) * aircraftParameters.takeoffSpeed;
		runwayQuery = default(RunwayQuery);
		runwayQuery.RunwayType = RunwayQueryType.Any;
		runwayQuery.MinSize = takeoffDistance;
		runwayQuery.TailHook = aircraft.weaponManager.HasTailHook();
		runwayQuery.LandingSpeed = landingSpeed;
		RunwayQuery runwayQuery2 = runwayQuery;
		nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position, runwayQuery2);
		if (nearestAirbase == null)
		{
			return;
		}
		if (!aircraftParameters.verticalLanding && FastMath.InRange(nearestAirbase.center.position, aircraft.transform.position, nearestAirbase.GetRadius() + 5000f))
		{
			runwayUsage = nearestAirbase.RequestLanding(aircraft, runwayQuery2);
			if (!landing && !aircraftParameters.verticalLanding && aircraft.radarAlt > 20f && aircraft.gearDeployed && runwayUsage.HasValue && runwayUsage.Value.Runway.AircraftOnApproach(aircraft, 2500f, excludeBetweenEndpoints: true) && Mathf.Abs(Vector3.Dot(aircraft.transform.forward, runwayUsage.Value.GetDirection())) > 0.8f)
			{
				string text = runwayUsage.Value.GetName();
				SceneSingleton<AircraftActionsReport>.i.ReportText("Cleared for landing on " + text, 10f);
				nearestAirbase.CmdRegisterUsage(aircraft, isUsing: true, runwayUsage.Value.Runway.index);
				ShowRunwayBorders(show: true);
				landing = true;
			}
			if (runwayUsage.HasValue && landing)
			{
				Airbase.Runway runway = runwayUsage.Value.Runway;
				if (aircraft.radarAlt < 0.2f)
				{
					landedAtRunway = runway;
					landing = false;
					runwayUsage = null;
					ShowRunwayBorders(show: false);
				}
				else if (aircraft.radarAlt > 10f && !runway.AircraftOnApproach(aircraft, 2500f, excludeBetweenEndpoints: false))
				{
					SceneSingleton<AircraftActionsReport>.i.ReportText("Aborted Landing", 5f);
					ShowRunwayBorders(show: false);
					runway.DeregisterLanding(aircraft);
					landing = false;
					runwayUsage = null;
				}
			}
		}
		else
		{
			runwayUsage = null;
		}
	}

	private bool PositionMarkers(Aircraft aircraft)
	{
		if (nearestAirbase == null)
		{
			return false;
		}
		Vector3 position = nearestAirbase.center.position;
		if (landing || (aircraft.radarAlt < 1f && !taxiingToRunway))
		{
			return false;
		}
		string text = nearestAirbase.SavedAirbase.DisplayName;
		if (!aircraft.pilots[0].flightInfo.HasTakenOff)
		{
			if (!taxiingToRunway || reachedRunway || !runwayUsage.HasValue)
			{
				return false;
			}
			Transform end = runwayUsage.Value.GetEnd();
			position = end.position;
			text = "Taxi to " + runwayName;
			if (FastMath.InRange(end.position, aircraft.transform.position, aircraft.maxRadius + 20f))
			{
				reachedRunway = true;
			}
		}
		Vector3 lhs = position - SceneSingleton<CameraStateManager>.i.transform.position;
		float magnitude = lhs.magnitude;
		if (Vector3.Dot(lhs, SceneSingleton<CameraStateManager>.i.transform.forward) < 0f)
		{
			return false;
		}
		if (HUDFunctions.PinToScreenEdge(position, out var rayToScreen, out var _))
		{
			airbaseMarker.transform.position = rayToScreen;
			airbaseLabel.transform.position = rayToScreen - rayToScreen.normalized * 50f;
		}
		else
		{
			airbaseMarker.transform.position = rayToScreen;
			airbaseLabel.transform.position = rayToScreen - Vector3.up * 20f;
		}
		airbaseLabel.text = text + " " + UnitConverter.DistanceReading(magnitude);
		airbaseLabel.fontSize = (int)PlayerSettings.overlayTextSize;
		return true;
	}

	private void DisplayMarkers(bool show)
	{
		if (show)
		{
			if (!airbaseMarker.enabled)
			{
				airbaseMarker.enabled = true;
				airbaseLabel.enabled = true;
			}
		}
		else if (airbaseMarker.enabled)
		{
			airbaseMarker.enabled = false;
			airbaseLabel.enabled = false;
		}
	}

	private void LateUpdate()
	{
		Aircraft aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		if (!(aircraft == null))
		{
			DisplayMarkers(PositionMarkers(aircraft));
			if (landing && runwayUsage.HasValue)
			{
				DrawRunwayBorders(runwayUsage.Value.Runway);
			}
			if (runwayUsage.HasValue && runwayUsage.Value.Runway != null && aircraft.gearDeployed)
			{
				glideslope.enabled = DrawGlideslope(aircraft, runwayUsage.Value);
				glideslopeAimPoint.enabled = glideslope.enabled;
			}
			else
			{
				glideslope.enabled = false;
				glideslopeAimPoint.enabled = false;
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
