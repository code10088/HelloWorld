using cfg;

namespace HotAssembly
{
	public class TriggerAction_ExcuteTrigger : ActionBase
	{
        private TriggerManager triggerManager;

        public TriggerAction_ExcuteTrigger(ActionConfig _config) : base(_config) { }

        public void Init(TriggerManager _triggerManager)
        {
            triggerManager = _triggerManager;
        }

        public override void Excute(params object[] param)
        {
            triggerManager.ExcuteTrigger((TriggerMode)config.IntParam[0]);
        }
    }
}
