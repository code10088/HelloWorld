using cfg;

namespace HotAssembly
{
	public class TriggerAction_AddBuff : ActionBase
	{
		public TriggerAction_AddBuff(ActionConfig _config) : base(_config) { }

        public override void Excute(params object[] param)
        {
            BuffManager buffManager = param[0] as BuffManager;
            buffManager.AddBuff(config.IntParam[0]);
        }
    }
}
