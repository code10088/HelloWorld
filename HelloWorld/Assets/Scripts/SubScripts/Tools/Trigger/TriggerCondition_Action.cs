using System;

namespace HotAssembly
{
	public class TriggerCondition_Action : TriggerConditionBase
	{
		private Func<bool> condition;

		public void Init(Func<bool> _condition)
		{
			condition = _condition;
		}

		public override bool CheckCondition()
		{
			return condition != null && condition.Invoke();
		}
	}
}
