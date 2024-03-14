namespace HotAssembly
{
	public class TriggerCondition_AccCount : TriggerConditionBase
	{
        public override bool CheckCondition()
        {
            return trigger.ExcuteCount >= config.Param[0];
        }
    }
}
