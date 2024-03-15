using cfg;

namespace HotAssembly
{
	public abstract class TriggerConditionBase
	{
		protected TriggerCondition config;

		public void Init(TriggerCondition _config)
		{
			config = _config;
		}
		public abstract bool CheckCondition();
	}
}
