using cfg;

namespace HotAssembly
{
	public abstract class TriggerActionBase
	{
		protected TriggerBase trigger;
		protected TriggerAction config;

		public void Init(TriggerBase _trigger, TriggerAction _config)
		{
			trigger = _trigger;
			config = _config;
		}
		public abstract void Excute();
	}
}
