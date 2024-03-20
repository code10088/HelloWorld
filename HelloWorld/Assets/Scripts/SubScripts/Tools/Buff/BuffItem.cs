using System.Collections.Generic;
using UnityEngine;
using cfg;
using System;

namespace HotAssembly
{
	public class BuffItem
	{
		private static int uniqueId = 0;

		private BuffManager buffManager;
		private int id;
		private Buff config;

		private int excuteCount = 0;//执行次数
		private int triggerCount = 0;//已触发次数
		private float totalEndTime = 0;
		private float cdEndTime = 0;
		private List<int> conditionKeys;
		private Dictionary<int, ConditionBase> conditions = new Dictionary<int, ConditionBase>();
		private List<ActionBase> actionList = new List<ActionBase>();

		public int BuffID => id;
		public int ConfigId => config.ID;
		public int Priority => config.Priority;//越大越先执行
		public int ExcuteCount => excuteCount;

		public void Init(BuffManager _buffManager, Buff _config, Func<bool> _condition, Action _action)
        {
			buffManager = _buffManager;
			id = ++uniqueId;
			config = _config;

			InitCondition(_condition);
			InitAction(_action);
			totalEndTime = Time.realtimeSinceStartup + config.TotalTime;
			cdEndTime = 0;
		}
		public bool Excute()
		{
			if (config.TotalTime > 0 && Time.realtimeSinceStartup > totalEndTime) return true;
			if (config.CDTime > 0 && Time.realtimeSinceStartup < cdEndTime) return false;
			excuteCount++;
			string str = config.Condition;
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int id = conditionKeys[i];
				bool b = conditions[id].CheckCondition();
				str = str.Replace(id.ToString(), b ? "t" : "f");
			}
			if (Utils.ParseBoolStr(str))
			{
				if (config.Count >= 0)
				{
					for (int i = 0; i < actionList.Count; i++) actionList[i].Excute();
					cdEndTime = Time.realtimeSinceStartup + config.CDTime;

					//触发次数
					triggerCount++;
					if (config.Count > 0 && triggerCount >= config.Count)
					{
						if (config.CountType == CommonHandleType1.Move) return true;
						else if (config.CountType == CommonHandleType1.Reset) triggerCount = 0;
					}
				}
			}
			return false;
		}

		private void InitCondition(Func<bool> _condition)
        {
			conditionKeys = Utils.SplitBoolStr(config.Condition);
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int a = conditionKeys[i];
				if (conditions.ContainsKey(a)) continue;
				ConditionBase temp = null;
				var conditionConfig = ConfigManager.Instance.GameConfigs.TbConditionConfig[a];
				switch (conditionConfig.ConditionType)
				{
					case "Condition_Action":
						var action = new Condition_Action(conditionConfig);
						action.Init(_condition);
						temp = action;
						break;
				}
				conditions.Add(a, temp);
			}
		}
		private void InitAction(Action _action)
        {
			for (int i = 0; i < config.Action.Count; i++)
			{
				ActionBase temp = null;
				int actionId = config.Action[i];
				var actionConfig = ConfigManager.Instance.GameConfigs.TbActionConfig[actionId];
				switch (actionConfig.ActionType)
				{
					case "Action_Action":
						var action = new Action_Action(actionConfig);
						action.Init(_action);
						temp = action;
						break;
					case "Action_Debug":
						temp = new Action_Debug(actionConfig);
						break;
					case "BuffAction_AddProp":
						temp = new BuffAction_AddProp(actionConfig);
						break;
				}
				actionList.Add(temp);
			}
		}
	}
}
