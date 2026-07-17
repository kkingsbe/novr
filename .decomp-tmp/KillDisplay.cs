using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class KillDisplay : SceneSingleton<KillDisplay>
{
	public enum MessageType
	{
		Kill,
		Recon,
		Jamming,
		Supply,
		Rescue,
		CapturePilots,
		CaptureLocation
	}

	[SerializeField]
	private Text killText;

	[SerializeField]
	private Text rankText;

	[SerializeField]
	private Image rankBackground;

	[SerializeField]
	private AudioClip[] killAudioEffects;

	[SerializeField]
	private AudioClip rankIncreaseClip;

	[SerializeField]
	private float maxKillHeat;

	[SerializeField]
	private AudioClip killsong;

	private float killHeat;

	private float killDisplayTimer;

	private float rankDisplayTimer;

	private float killsongLastPlayed = -60f;

	private void OnEnable()
	{
		rankText.enabled = false;
		rankBackground.enabled = false;
		killsongLastPlayed = -60f;
	}

	public void DisplayKill(PersistentUnit killedUnit, float creditGiven, FactionHQ.RewardType actionType)
	{
		killDisplayTimer = 5f;
		killText.enabled = true;
		if (actionType == FactionHQ.RewardType.Kill && killedUnit != null)
		{
			killHeat += creditGiven;
			killText.text = killedUnit.unitName + " +" + creditGiven.ToString("F1");
			int num = Mathf.FloorToInt(Mathf.Clamp(killHeat / maxKillHeat, 0f, 0.99f) * (float)killAudioEffects.Length);
			SoundManager.PlayInterfaceOneShot(killAudioEffects[num]);
			base.enabled = true;
			if (killHeat >= maxKillHeat && Time.timeSinceLevelLoad - killsongLastPlayed > 60f)
			{
				killsongLastPlayed = Time.timeSinceLevelLoad;
				MusicManager.i.CrossFadeMusic(killsong, 2f, 0f, repeat: false, allowReplay: true, replacePlaying: false);
			}
			return;
		}
		switch (actionType)
		{
		case FactionHQ.RewardType.Recon:
			killText.text = "Detected units +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.Jamming:
			killText.text = "Jamming radar +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.Supply:
			killText.text = "Resupplied units +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.Refuel:
			killText.text = "Resupplied units +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.Repair:
			killText.text = "Repaired unit +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.RescuePilots:
			killText.text = "Rescued pilots +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.CapturePilots:
			killText.text = "Captured pilots +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		case FactionHQ.RewardType.CaptureLocation:
			killText.text = "Captured location +" + creditGiven.ToString("F1");
			base.enabled = true;
			break;
		}
	}

	public void DisplayBonus(float scoreAwarded)
	{
		if (scoreAwarded != 0f)
		{
			killDisplayTimer = 5f;
			killText.enabled = true;
			killText.text = $"Successful Sortie + {scoreAwarded:F1}";
			base.enabled = true;
		}
	}

	public static void FlashRank(int rank)
	{
		if (SceneSingleton<KillDisplay>.i == null)
		{
			Object.Instantiate(GameAssets.i.killDisplay, SceneSingleton<GameplayUI>.i.gameplayCanvas.transform);
		}
		SceneSingleton<KillDisplay>.i.DelayFlashRank(rank).Forget();
	}

	public static void FlashNewAircraft(AircraftDefinition aircraftDefinition)
	{
		if (SceneSingleton<KillDisplay>.i == null)
		{
			Object.Instantiate(GameAssets.i.killDisplay, SceneSingleton<GameplayUI>.i.gameplayCanvas.transform);
		}
		SceneSingleton<KillDisplay>.i.DelayFlashNewAircraft(aircraftDefinition).Forget();
	}

	private async UniTask DelayFlashRank(int rank)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(2000);
		if (!cancel.IsCancellationRequested)
		{
			base.enabled = true;
			rankDisplayTimer = 5f;
			rankText.enabled = true;
			rankBackground.enabled = true;
			SoundManager.PlayInterfaceOneShot(rankIncreaseClip);
			rankText.text = $"RANK {rank}";
		}
	}

	private async UniTask DelayFlashNewAircraft(AircraftDefinition aircraftDefinition)
	{
		if (!base.destroyCancellationToken.IsCancellationRequested)
		{
			base.enabled = true;
			rankDisplayTimer = 5f;
			rankText.enabled = true;
			rankBackground.enabled = true;
			SoundManager.PlayInterfaceOneShot(rankIncreaseClip);
			rankText.text = "+1 " + aircraftDefinition.unitName;
		}
	}

	private void Update()
	{
		if (killDisplayTimer <= 0f && rankDisplayTimer <= 0f && killHeat <= 0f)
		{
			base.enabled = false;
			killHeat = 0f;
			return;
		}
		killHeat -= Time.deltaTime;
		killDisplayTimer -= Time.deltaTime;
		if (killDisplayTimer <= 0f)
		{
			killText.enabled = false;
		}
		rankDisplayTimer -= Time.deltaTime;
		if (rankDisplayTimer <= 0f)
		{
			rankText.enabled = false;
			rankBackground.enabled = false;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
