using NuclearOption.Networking;
using TMPro;
using UnityEngine;

public class TeamKillPanel : SceneSingleton<TeamKillPanel>
{
	[SerializeField]
	private TMP_Text teamkillText;

	[SerializeField]
	private Transform panel;

	private Player suspectPlayer;

	protected override void Awake()
	{
		base.Awake();
		panel.gameObject.SetActive(value: false);
	}

	public void ShowTeamKillPanel(Player teamKiller)
	{
		teamkillText.text = "Teamkilled by " + teamKiller.PlayerName;
		suspectPlayer = teamKiller;
	}

	public void KickPlayer()
	{
		if (suspectPlayer != null && GameManager.GetLocalPlayer<Player>(out var _))
		{
			Debug.LogWarning("KickPlayer Not Implemented");
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
