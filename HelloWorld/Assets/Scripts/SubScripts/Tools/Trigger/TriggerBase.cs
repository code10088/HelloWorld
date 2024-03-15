using System.Collections.Generic;
using UnityEngine;
using cfg;
using System;

namespace HotAssembly
{
	public class TriggerBase
	{
		private static int uniqueId = 0;
		private readonly string[] splitStr = new string[] { "(", ")", "!", "&&", "||" };

		private TriggerManager triggerManager;
		private int id;
		private TriggerMode triggerMode;
		private Trigger config;

		private int count = 0;//执行次数
		private int count1 = 0;//已触发次数
		private int count2 = 0;//已触发次数
		private float totalEndTime = 0;
		private float cdEndTime = 0;
		private List<int> conditionKeys;
		private Dictionary<int, TriggerConditionBase> conditions = new Dictionary<int, TriggerConditionBase>();
		private List<TriggerActionBase> actionList1 = new List<TriggerActionBase>();//正触发行为
		private List<TriggerActionBase> actionList2 = new List<TriggerActionBase>();//反触发行为

		public int TriggerID => id;
		public int ConfigId => config.ID;
		public TriggerMode TriggerMode => triggerMode;
		public int Priority => config.Priority;//越大越先执行
		public int ExcuteCount => count;

		public virtual void Init(TriggerManager _triggerManager, Trigger _config, Func<bool> _condition, Action _action1, Action _action2)
        {
			triggerManager = _triggerManager;
			id = ++uniqueId;
			config = _config;

			InitCondition(_condition);
			InitAction(_action1, _action2);
			totalEndTime = Time.realtimeSinceStartup + config.TotalTime;
			cdEndTime = 0;
		}
		public virtual bool Excute(params object[] param)
		{
			if (config.TotalTime > 0 && Time.realtimeSinceStartup > totalEndTime) return true;
			if (config.CDTime > 0 && Time.realtimeSinceStartup < cdEndTime) return false;
			count++;
			string str = config.Condition;
			for (int i = 0; i < conditionKeys.Count; i++)
			{
				int id = conditionKeys[i];
				bool b = conditions[id].CheckCondition();
				str = str.Replace(id.ToString(), b ? "t" : "f");
			}
			if (Utils.ParseBoolStr(str))
			{
				if (config.Count1 >= 0)
				{
					for (int i = 0; i < actionList1.Count; i++) actionList1[i].Excute();
					cdEndTime = Time.realtimeSinceStartup + config.CDTime;

					//触发次数
					count1++;
					if (config.Count1 > 0 && count1 >= config.Count1)
					{
						if (config.Count1Type == CommonHandleType1.Move) return true;
						else if (config.Count1Type == CommonHandleType1.Reset) count1 = 0;
					}
				}
			}
			else
			{
				if (config.Count2 >= 0)
				{
					for (int i = 0; i < actionList2.Count; i++) actionList2[i].Excute();
					cdEndTime = Time.realtimeSinceStartup + config.CDTime;

					//触发次数
					count2++;
					if (config.Count2 > 0 && count2 >= config.Count2)
					{
						if (config.Count2Type == CommonHandleType1.Move) return true;
						else if (config.Count2Type == CommonHandleType1.Reset) count2 = 0;
					}
				}
			}
			return false;
		}

		private void InitCondition(Func<bool> _condition)
        {
			var strs = config.Condition.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < strs.Length; i++)
			{
				int a = int.Parse(strs[i]);
				if (conditions.ContainsKey(a)) continue;
				TriggerConditionBase temp = null;
				var conditionConfig = ConfigManager.Instance.GameConfigs.TbTriggerCondition[a];
				switch (conditionConfig.TriggerType)
				{
					case "TriggerCondition_Action":
						var action = new TriggerCondition_Action();
						action.Init(_condition);
						temp = action;
						break;
					case "TriggerCondition_Random":
						var random = new TriggerCondition_Random();
						random.Init(this, conditionConfig);
						temp = random;
						break;
					case "TriggerCondition_AccCount":
						var acc = new TriggerCondition_AccCount();
						acc.Init(this, conditionConfig);
						temp = acc;
						break;
				}
				conditions.Add(a, temp);
			}
			conditionKeys = new List<int>(conditions.Keys);
			conditionKeys.Sort((a,b)=> { return b - a; });
		}
		private void InitAction(Action _action1, Action _action2)
        {
			List<int> actionList = new List<int>();
			actionList.AddRange(config.Action1);
			actionList.AddRange(config.Action2);
			for (int i = 0; i < actionList.Count; i++)
			{
				TriggerActionBase temp = null;
				int index = i - config.Action1.Count;
				int actionId = index < 0 ? config.Action1[i] : config.Action2[index];
				var actionConfig = ConfigManager.Instance.GameConfigs.TbTriggerAction[actionId];
				switch (actionConfig.TriggerType)
				{
					case "TriggerAction_AddTrigger":
						var addTrigger = new TriggerAction_AddTrigger();
						addTrigger.Init(this, actionConfig);
						addTrigger.Init(triggerManager);
						temp = addTrigger;
						break;
					case "TriggerAction_RemoveTrigger":
						var removeTrigger = new TriggerAction_RemoveTrigger();
						removeTrigger.Init(this, actionConfig);
						removeTrigger.Init(triggerManager);
						temp = removeTrigger;
						break;
					case "TriggerAction_ExcuteTrigger":
						var excuteTrigger = new TriggerAction_ExcuteTrigger();
						excuteTrigger.Init(this, actionConfig);
						excuteTrigger.Init(triggerManager);
						temp = excuteTrigger;
						break;
					case "TriggerAction_AddBuff":
						var addBuff = new TriggerAction_AddBuff();
						addBuff.Init(this, actionConfig);
						temp = addBuff;
						break;
					case "TriggerAction_RemoveBuff":
						var removeBuff = new TriggerAction_RemoveBuff();
						removeBuff.Init(this, actionConfig);
						temp = removeBuff;
						break;
					case "TriggerAction_Action":
						var action = new TriggerAction_Action();
						action.Init(index < 0 ? _action1 : _action2);
						temp = action;
						break;
				}
				if (index < 0) actionList1.Add(temp);
				else actionList2.Add(temp);
			}
		}
	}
}
