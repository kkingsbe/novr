using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : SceneSingleton<GameplayUI>
{
	public Image hurt;

	public Canvas gameplayCanvas;

	public Canvas menuCanvas;

	public float pilotHitPoints;

	public DialogueBox DialogueBox;

	public MessageUI MessageUI;

	public Transform topPanelTransform;

	[SerializeField]
	private GameObject selectAirbasePanel;

	[SerializeField]
	private TMP_Text airbaseName;

	[SerializeField]
	private Button selectAircraftButton;

	[SerializeField]
	private GameObject aircraftSelectionMenu;

	[SerializeField]
	private GameObject joinMenu;

	[SerializeField]
	private GameObject spectatorPanel;

	private Airbase homeAirbase;

	private float lastResumed;

	private Rewired.Player player;

	[SerializeField]
	private GameObject factionInfoPanel_BDF;

	[SerializeField]
	private GameObject factionInfoPanel_PALA;

	public static bool GameIsPaused { get; private set; } = false;


	public static bool GameSlowMotion { get; private set; } = false;


	public static bool AllowPauseKeybind { get; set; } = true;


	protected override void Awake()
	{
		base.Awake();
		GameIsPaused = false;
		GameSlowMotion = false;
		AllowPauseKeybind = true;
		player = ReInput.players.GetPlayer(0);
		HideSpectatorPanel();
	}

	public void ShowSelectAirbase()
	{
		selectAirbasePanel.SetActive(value: true);
		bool active = false;
		if (GameManager.gameResolution == GameResolution.Ongoing && GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer) && localPlayer.HQ != null && (SceneSingleton<CombatHUD>.i.aircraft == null || SceneSingleton<CombatHUD>.i.aircraft.disabled) && !localPlayer.AircraftSpawnPending && homeAirbase != null)
		{
			airbaseName.text = homeAirbase.SavedAirbase.DisplayName;
			active = true;
		}
		if (GameManager.gameResolution == GameResolution.Defeat)
		{
			airbaseName.text = "Mission Failed, no spawn points available ";
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(selectAirbasePanel.GetComponent<RectTransform>());
		selectAircraftButton.gameObject.SetActive(active);
	}

	public void HideSelectAirbase()
	{
		selectAirbasePanel.SetActive(value: false);
	}

	public void ShowJoinMenu()
	{
		SceneSingleton<DynamicMap>.i.Minimize();
		joinMenu.SetActive(value: true);
	}

	public void ShowSpectatorPanel()
	{
		if (GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer) && localPlayer.HQ == null)
		{
			spectatorPanel.SetActive(value: true);
			HideSelectAirbase();
		}
	}

	public void HideSpectatorPanel()
	{
		spectatorPanel.SetActive(value: false);
	}

	public void SpectatorPanelDeselectAll()
	{
		factionInfoPanel_BDF.GetComponent<InfoPanel_Faction>().DeselectPlayers();
		factionInfoPanel_PALA.GetComponent<InfoPanel_Faction>().DeselectPlayers();
	}

	public void HideJoinMenu()
	{
		joinMenu.SetActive(value: false);
	}

	public void GameMessage(string message)
	{
		MessageUI.GameMessage(message);
	}

	public void GameMessage(string message, float delay)
	{
		MessageUI.DelayedGameMessage(message, delay).Forget();
	}

	public void KillFeed(string message)
	{
		MessageUI.KillFeed(message);
	}

	public void SelectAirbase(Airbase airbase)
	{
		homeAirbase = airbase;
		selectAirbasePanel.SetActive(value: true);
		airbaseName.text = airbase.SavedAirbase.DisplayName;
		selectAircraftButton.gameObject.SetActive(value: true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(selectAirbasePanel.GetComponent<RectTransform>());
	}

	public void SelectAircraft()
	{
		if (!GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			Debug.LogError("SelectAircraft was clicked but no local player");
			return;
		}
		FactionHQ hQ = localPlayer.HQ;
		if (hQ == null)
		{
			Debug.LogError("SelectAircraft was clicked without local faction");
		}
		else if (hQ != homeAirbase.CurrentHQ)
		{
			Debug.LogWarning("SelectAircraft was clicked but airbase faction was not local faction");
		}
		else
		{
			Object.Instantiate(aircraftSelectionMenu, gameplayCanvas.transform).GetComponent<AircraftSelectionMenu>().Initialize(localPlayer, homeAirbase);
		}
	}

	public void FlashHurt(float damage, float remainingHitPoints)
	{
		pilotHitPoints = remainingHitPoints;
		hurt.gameObject.SetActive(value: true);
		float a = hurt.color.a;
		a += Mathf.Max(damage * 0.01f, 0.2f);
		hurt.color = new Color(1f, 1f, 1f, a);
	}

	public void PauseGame()
	{
		if (!GameIsPaused)
		{
			GameIsPaused = true;
			CursorManager.SetFlag(CursorFlags.Pause, value: true);
			menuCanvas.enabled = true;
			SceneSingleton<DynamicMap>.i.Minimize();
			gameplayCanvas.enabled = false;
			FlightHud.EnableCanvas(enable: false);
			Object.Instantiate(GameAssets.i.leaderboard, menuCanvas.transform);
		}
	}

	public void ResumeGame()
	{
		if (GameIsPaused)
		{
			GameIsPaused = false;
			CursorManager.SetFlag(CursorFlags.Pause, value: false);
			menuCanvas.enabled = false;
			gameplayCanvas.enabled = true;
			if (SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState)
			{
				FlightHud.EnableCanvas(enable: true);
			}
			if (SceneSingleton<CombatHUD>.i.aircraft == null)
			{
				SceneSingleton<DynamicMap>.i.Maximize();
			}
			lastResumed = Time.timeSinceLevelLoad;
		}
	}

	private void OnDestroy()
	{
		GameIsPaused = false;
		CursorManager.SetFlag(CursorFlags.Pause, value: false);
	}

	public void Update()
	{
		if (hurt.gameObject.activeSelf)
		{
			float a = hurt.color.a;
			a -= 0.002f * Time.deltaTime * Mathf.Max(pilotHitPoints, 10f);
			a = Mathf.Clamp01(a);
			hurt.color = new Color(1f, 1f, 1f, a);
			if (a == 0f)
			{
				hurt.gameObject.SetActive(value: false);
			}
		}
		if (AllowPauseKeybind && (GameManager.playerInput.GetButtonDown("Pause") || Input.GetKeyDown(KeyCode.Escape)) && !GameIsPaused && Time.timeSinceLevelLoad - lastResumed > 0.1f)
		{
			lastResumed = Time.timeSinceLevelLoad;
			if (GameManager.gameState == GameState.Multiplayer || GameManager.gameState == GameState.SinglePlayer)
			{
				PauseGame();
			}
		}
		if (Application.isEditor && GameManager.gameState == GameState.SinglePlayer && Input.GetKeyDown(KeyCode.Y))
		{
			TimeScaleManager.Scale = 4f;
		}
		if (GameManager.gameState == GameState.SinglePlayer && GameManager.playerInput.GetButtonDown("Slow Motion"))
		{
			GameSlowMotion = !GameSlowMotion;
			GameMessage(GameSlowMotion ? "Slow Motion Enabled" : "Slow Motion Disabled");
			if (GameIsPaused)
			{
				return;
			}
			SetTimeFactor(GameSlowMotion ? 0.05f : 1f);
		}
		if (!(SceneSingleton<CombatHUD>.i.aircraft != null) && SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.selectionState)
		{
			int num = 0;
			int num2 = 0;
			if (num2 != 0 || num != 0)
			{
				SwitchSpectatedAircraft(num, num2);
			}
		}
	}

	public void SwitchSpectatedAircraft(int facChange, int pChange)
	{
		List<Aircraft> list = new List<Aircraft>();
		List<Aircraft> list2 = new List<Aircraft>();
		CameraStateManager cam = SceneSingleton<CameraStateManager>.i;
		int num = 0;
		int num2 = 0;
		FactionHQ factionHQ = null;
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			if (allUnit is Aircraft item && (SceneSingleton<DynamicMap>.i.HQ == null || (SceneSingleton<DynamicMap>.i.HQ != null && SceneSingleton<DynamicMap>.i.HQ.IsTargetBeingTracked(allUnit))))
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		if (cam.followingUnit != null && cam.followingUnit is Aircraft { NetworkHQ: var networkHQ } aircraft)
		{
			foreach (Aircraft item2 in list)
			{
				if ((facChange == 0 && item2.NetworkHQ == networkHQ) || (facChange != 0 && item2.NetworkHQ != networkHQ))
				{
					list2.Add(item2);
				}
			}
			if (facChange != 0)
			{
				list2 = list2.OrderBy((Aircraft _x) => FastMath.Distance(_x.transform.GlobalPosition(), cam.transform.GlobalPosition())).ToList();
			}
			num = (list2.Contains(aircraft) ? list2.IndexOf(aircraft) : 0);
		}
		else
		{
			list2 = list;
		}
		if (list2.Count != 0)
		{
			num2 = num + pChange;
			if (num2 > list2.Count - 1)
			{
				num2 = 0;
			}
			else if (num2 < 0)
			{
				num2 = list2.Count - 1;
			}
			cam.SetFollowingUnit(list2[num2]);
			SceneSingleton<DynamicMap>.i.DeselectAllIcons();
			SceneSingleton<DynamicMap>.i.SelectIcon(list2[num2]);
		}
	}

	public void SetTimeFactor(float value)
	{
		TimeScaleManager.Scale = value;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
