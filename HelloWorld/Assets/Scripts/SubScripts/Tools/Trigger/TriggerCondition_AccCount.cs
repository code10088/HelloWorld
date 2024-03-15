namespace HotAssembly
{
	public class TriggerCondition_AccCount : TriggerConditionBase
    {
        private TriggerBase trigger;

        public void Init(TriggerBase _trigger)
        {
            trigger = _trigger;
        }

        public override bool CheckCondition()
        {
            return trigger.ExcuteCount >= config.Param[0];
        }
    }
}
