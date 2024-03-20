using cfg;

namespace HotAssembly
{
	public class TriggerCondition_AccCount : ConditionBase
    {
        private TriggerItem trigger;

        public TriggerCondition_AccCount(ConditionConfig _config) : base(_config) { }

        public void Init(TriggerItem _trigger)
        {
            trigger = _trigger;
        }

        public override bool CheckCondition(params object[] param)
        {
            return trigger.ExcuteCount >= config.IntParam[0];
        }
    }
}
