using UnityEngine;

namespace HotAssembly
{
	public class TriggerCondition_Random : TriggerConditionBase
	{
        public override bool CheckCondition()
        {
            int random = Random.Range(0, 100);
            return random < config.Param[0];
        }
    }
}
