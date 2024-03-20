using cfg;

namespace HotAssembly
{
	public abstract class ActionBase
	{
		protected ActionConfig config;

		public ActionBase(ActionConfig _config)
		{
			config = _config;
		}
		public abstract void Excute(params object[] param);
	}
}
