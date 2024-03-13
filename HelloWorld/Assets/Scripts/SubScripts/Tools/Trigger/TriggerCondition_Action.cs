using cfg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
	public class TriggerCondition_Action : TriggerConditionBase
	{
		public TriggerCondition_Action(TriggerCondition _config) : base(_config) { }

        public void Init(Func<bool> _condition)
		{

		}

		public override bool CheckCondition()
		{
			throw new NotImplementedException();
		}

	}
}
