using cfg;
using UnityEngine;

namespace HotAssembly
{
	public class TriggerCondition_Random : ConditionBase
	{
        public TriggerCondition_Random(ConditionConfig _config) : base(_config) { }

        public override bool CheckCondition(params object[] param)
        {
            int random = Random.Range(0, 100);
            return random < config.IntParam[0];
        }
    }
}
