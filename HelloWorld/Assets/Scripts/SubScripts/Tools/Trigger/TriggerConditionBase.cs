using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
	public abstract class TriggerConditionBase
	{
		protected TriggerCondition config;
		public TriggerConditionBase(TriggerCondition _config)
        {
			config = _config;
		}
		public void Init() { }
		public abstract bool CheckCondition();
	}
}
