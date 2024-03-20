using cfg;

namespace HotAssembly
{
	public class TriggerAction_RemoveBuff : ActionBase
    {
        public TriggerAction_RemoveBuff(ActionConfig _config) : base(_config) { }

        public override void Excute(params object[] param)
        {
            BuffManager buffManager = param[0] as BuffManager;
            buffManager.RemoveBuffByConfig(config.IntParam[0]);
        }
    }
}
