using System.Collections.Generic;
using UnityEngine;
using cfg;
using System;

namespace HotAssembly
{
	public class TriggerItem
	{
		private static int uniqueId = 0;
		private int id;
		private bool endMark = false;

		private TriggerManager triggerManager;
		private Trigger config;

		private int excuteCount = 0;//执行次数
		private int triggerCount1 = 0;//已触发次数
		private int triggerCount2 = 0;//已触发次数
		private float totalEndTime = 0;
		private float cdEndTime = 0;
		private List<int> conditionKeys;
		private Dictionary<int, ConditionBase> conditions = new Dictionary<int, ConditionBase>();
		private List<ActionBase> actionList1 = new List<ActionBase>();//正触发行为
		private List<ActionBase> actionList2 = new List<ActionBase>();//反触发行为

		public int TriggerID => id;
		public bool EndMark => endMark;
		public int ConfigId => config.ID;
		public int Priority => config.Priority;//越大越先执行
		public int ExcuteCount => excuteCount;

		public void Init(TriggerManager _triggerManager, Trigger _config, Func<bool> _condition, Action _action1, Action _action2)
        {
			triggerManager = _triggerManager;
			id = ++uniqueId;
			config = _config;

			InitCondition(_condition);
			InitAction(_action1, _action2);
			totalEndTime = Time.realtimeSinceStartup + config.TotalTime;
			cdEndTime = 0;
		}
		public void Excute(params object[] param)
		{
			if (endMark)
			{
				return;
			}
			if (config.TotalTime > 0 && Time.realtimeSinceStartup > totalEndTime)
			{
				Remove();
				return;
			}
			if (config.CDTime > 0 && Time.realtimeSinceStartup < cdEndTime)
			{
				return;
			}
			excuteCount++;
			string str = config.Condition;
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int id = conditionKeys[i];
				bool b = conditions[id].CheckCondition(param);
				str = str.Replace(id.ToString(), b ? "t" : "f");
			}
			if (Utils.ParseBoolStr(str))
			{
				if (config.Count1 >= 0)
				{
					for (int i = 0; i < actionList1.Count; i++) actionList1[i].Excute(param);
					cdEndTime = Time.realtimeSinceStartup + config.CDTime;

					//触发次数
					triggerCount1++;
					if (config.Count1 > 0 && triggerCount1 >= config.Count1)
					{
						if (config.Count1Type == CommonHandleType1.Move) Remove();
						else if (config.Count1Type == CommonHandleType1.Reset) triggerCount1 = 0;
					}
				}
			}
			else
			{
				if (config.Count2 >= 0)
				{
					for (int i = 0; i < actionList2.Count; i++) actionList2[i].Excute(param);
					cdEndTime = Time.realtimeSinceStartup + config.CDTime;

					//触发次数
					triggerCount2++;
					if (config.Count2 > 0 && triggerCount2 >= config.Count2)
					{
						if (config.Count2Type == CommonHandleType1.Move) Remove();
						else if (config.Count2Type == CommonHandleType1.Reset) triggerCount2 = 0;
					}
				}
			}
		}
		public void Remove()
        {
			endMark = true;
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
					case ConditionType.Condition_Action:
						var action = new Condition_Action(conditionConfig);
						action.Init(_condition);
						temp = action;
						break;
					case ConditionType.TriggerCondition_Random:
						temp = new TriggerCondition_Random(conditionConfig);
						break;
					case ConditionType.TriggerCondition_AccCount:
						var acc = new TriggerCondition_AccCount(conditionConfig);
						acc.Init(this);
						temp = acc;
						break;
				}
				conditions.Add(a, temp);
			}
		}
		private void InitAction(Action _action1, Action _action2)
        {
			List<int> actionList = new List<int>();
			actionList.AddRange(config.Action1);
			actionList.AddRange(config.Action2);
			for (int i = 0; i < actionList.Count; i++)
			{
				ActionBase temp = null;
				int index = i - config.Action1.Count;
				int actionId = index < 0 ? config.Action1[i] : config.Action2[index];
				var actionConfig = ConfigManager.Instance.GameConfigs.TbActionConfig[actionId];
				switch (actionConfig.ActionType)
				{
					case ActionType.Action_Action:
						var action = new Action_Action(actionConfig);
						action.Init(index < 0 ? _action1 : _action2);
						temp = action;
						break;
					case ActionType.Action_Debug:
						temp = new Action_Debug(actionConfig);
						break;
					case ActionType.TriggerAction_AddTrigger:
						var addTrigger = new TriggerAction_AddTrigger(actionConfig);
						addTrigger.Init(triggerManager);
						temp = addTrigger;
						break;
					case ActionType.TriggerAction_RemoveTrigger:
						var removeTrigger = new TriggerAction_RemoveTrigger(actionConfig);
						removeTrigger.Init(triggerManager);
						temp = removeTrigger;
						break;
					case ActionType.TriggerAction_ExcuteTrigger:
						var excuteTrigger = new TriggerAction_ExcuteTrigger(actionConfig);
						excuteTrigger.Init(triggerManager);
						temp = excuteTrigger;
						break;
					case ActionType.TriggerAction_AddBuff:
						temp = new TriggerAction_AddBuff(actionConfig);
						break;
					case ActionType.TriggerAction_RemoveBuff:
						temp = new TriggerAction_RemoveBuff(actionConfig);
						break;
				}
				if (index < 0) actionList1.Add(temp);
				else actionList2.Add(temp);
			}
		}
	}
}
