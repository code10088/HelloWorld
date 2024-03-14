using cfg;
using System;

namespace HotAssembly
{
	public class TriggerAction_Action : TriggerActionBase
	{
		public Action action;

		public void Init(Action _action)
		{
			action = _action;
		}

		public override void Excute()
		{
			action?.Invoke();
		}
	}
}
