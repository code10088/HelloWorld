using cfg;
using System;

public class Action_Action : ActionBase
{
	public Action action;

	public Action_Action(ActionConfig _config) : base(_config) { }

	public void Init(Action _action)
	{
		action = _action;
	}

	public override void Excute(params object[] param)
	{
		action?.Invoke();
	}
}