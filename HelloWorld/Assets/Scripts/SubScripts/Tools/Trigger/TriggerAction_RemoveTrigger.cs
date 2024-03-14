namespace HotAssembly
{
	public class TriggerAction_RemoveTrigger : TriggerActionBase
	{
        private TriggerManager triggerManager;

        public void Init(TriggerManager _triggerManager)
        {
            triggerManager = _triggerManager;
        }

        public override void Excute()
        {
            triggerManager.RemoveTriggerByConfig((int)config.Param[0]);
        }
    }
}
