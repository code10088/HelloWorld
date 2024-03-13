using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
	public class TriggerActionBase
	{
		protected TriggerAction config;
		public TriggerActionBase(TriggerAction _config)
		{
			config = _config;
		}
		public void Init()
		{

		}
	}
}
