using cfg;
using UnityEngine;

public class Action_Debug : ActionBase
{
	public Action_Debug(ActionConfig _config) : base(_config) { }

	public override void Excute(params object[] param)
	{
		GameDebug.Log("Action: " + TimeUtils.ServerTime);
	}
}