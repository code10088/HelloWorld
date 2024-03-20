using cfg;

namespace HotAssembly
{
	public class TriggerAction_RemoveTrigger : ActionBase
	{
        private TriggerManager triggerManager;

        public TriggerAction_RemoveTrigger(ActionConfig _config) : base(_config) { }

        public void Init(TriggerManager _triggerManager)
        {
            triggerManager = _triggerManager;
        }

        public override void Excute(params object[] param)
        {
            triggerManager.RemoveTriggerByConfig(config.IntParam[0]);
        }
    }
}
