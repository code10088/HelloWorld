using cfg;

namespace HotAssembly
{
	public abstract class TriggerActionBase
	{
		protected TriggerAction config;

		public void Init(TriggerAction _config)
		{
			config = _config;
		}
		public abstract void Excute();
	}
}
