namespace HotAssembly
{
	public class TriggerAction_AddTrigger : TriggerActionBase
	{
        private TriggerManager triggerManager;

        public void Init(TriggerManager _triggerManager)
        {
            triggerManager = _triggerManager;
        }

        public override void Excute()
        {
            triggerManager.AddTrigger((int)config.Param[0]);
        }
    }
}
