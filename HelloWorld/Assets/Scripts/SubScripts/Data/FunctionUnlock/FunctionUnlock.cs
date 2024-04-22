using cfg;
using System.Collections.Generic;

namespace HotAssembly
{
	public class FunctionUnlock : Singletion<FunctionUnlock>
	{
		/// <summary>
		/// ½ûÖ¹Æµ·±µ÷ÓÃ
		/// </summary>
		public bool CheckUnlock(FunctionUnlockType type)
		{
			var config = ConfigManager.Instance.GameConfigs.TbFunctionUnlockConfig[type];
			string str = config.Condition;
			var conditionKeys = Utils.SplitBoolStr(str);
			var conditions = InitCondition(conditionKeys);
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int id = conditionKeys[i];
				bool b = conditions[id].CheckCondition();
				str = str.Replace(id.ToString(), b ? "t" : "f");
			}
			return Utils.ParseBoolStr(str);
		}
		private Dictionary<int, ConditionBase> InitCondition(List<int> conditionKeys)
		{
			var conditions = new Dictionary<int, ConditionBase>();
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int a = conditionKeys[i];
				if (conditions.ContainsKey(a)) continue;
				ConditionBase temp = null;
				var conditionConfig = ConfigManager.Instance.GameConfigs.TbConditionConfig[a];
				switch (conditionConfig.ConditionType)
				{
					case ConditionType.FunctionUnlockCondition_Lv:
						temp = new FunctionUnlockCondition_Lv(conditionConfig);
						break;
				}
				conditions.Add(a, temp);
			}
			return conditions;
		}
	}
}
