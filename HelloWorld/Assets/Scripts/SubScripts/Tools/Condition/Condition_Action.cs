using cfg;
using System;

public class Condition_Action : ConditionBase
{
	private Func<bool> condition;

	public Condition_Action(ConditionConfig _config) : base(_config) { }

	public void Init(Func<bool> _condition)
	{
		condition = _condition;
	}

	public override bool CheckCondition(params object[] param)
	{
		return condition != null && condition.Invoke();
	}
}