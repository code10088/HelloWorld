using cfg;

namespace HotAssembly
{
	public class TriggerAction_ExcuteTrigger : TriggerActionBase
	{
        private TriggerManager triggerManager;

        public void Init(TriggerManager _triggerManager)
        {
            triggerManager = _triggerManager;
        }

        public override void Excute()
        {
            triggerManager.ExcuteTrigger((TriggerMode)config.Param[0]);
        }
    }
}
