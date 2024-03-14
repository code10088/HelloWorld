using cfg;

namespace HotAssembly
{
	public abstract class TriggerConditionBase
	{
		protected TriggerBase trigger;
		protected TriggerCondition config;

		public void Init(TriggerBase _trigger, TriggerCondition _config)
		{
			trigger = _trigger;
			config = _config;
		}
		public abstract bool CheckCondition();
	}
}
