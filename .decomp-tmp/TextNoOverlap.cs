using UnityEngine;
using UnityEngine.UI;

public class TextNoOverlap
{
	public readonly Text Text;

	public Vector2 TargetPosition;

	public Vector2 PreviousPosition;

	public Vector2 NudgeOffset;

	public bool AutomaticlalySetPosition;

	public TextNoOverlap(Text objectiveInfo)
	{
		Text = objectiveInfo;
	}

	public void SetTarget(Vector2 target)
	{
		TargetPosition = target;
		if (AutomaticlalySetPosition)
		{
			Text.transform.position = target;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '8.2.0.7535-95108c96')
