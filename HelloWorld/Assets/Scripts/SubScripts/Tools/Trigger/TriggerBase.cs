using System.Collections;
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

		private int id;
		private TriggerMode triggerMode;
		private Trigger config;

		private int count1 = 0;//正触发次数
		private int count2 = 0;//反触发次数
		private int count3 = 0;//正累积次数
		private int count4 = 0;//反累积次数
		private float timer = 0;
		private float cdTimer = 0;
		private Dictionary<int, TriggerConditionBase> conditions = new Dictionary<int, TriggerConditionBase>();
		private List<TriggerActionBase> actionList1 = new List<TriggerActionBase>();//正触发行为
		private List<TriggerActionBase> actionList2 = new List<TriggerActionBase>();//反触发行为

		public int TriggerID => id;
		public int ConfigId => config.ID;
		public TriggerMode TriggerMode => triggerMode;
		public int Priority => config.Priority;//越大越先执行

		public virtual void Init(Trigger _config, Func<bool> _condition, Action _action1, Action _action2)
        {
			id = ++uniqueId;
			config = _config;

			InitCondition(_condition);
			InitAction(_action1, _action2);
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
						var action = new TriggerCondition_Action(conditionConfig);
						action.Init(_condition);
						temp = action;
						break;
					case "TriggerCondition_Random":
						var random = new TriggerCondition_Random(conditionConfig);
						random.Init();
						temp = random;
						break;
					case "TriggerCondition_AccCount":
						var acc = new TriggerCondition_AccCount(conditionConfig);
						acc.Init();
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
				TriggerActionBase temp = null;
				var actionConfig = ConfigManager.Instance.GameConfigs.TbTriggerAction[config.Action1[i]];
				switch (actionConfig.TriggerType)
				{
					case "TriggerAction_AddTrigger":
						var addTrigger = new TriggerAction_AddTrigger(actionConfig);
						addTrigger.Init();
						temp = addTrigger;
						break;
					case "TriggerAction_RemoveTrigger":
						var removeTrigger = new TriggerAction_RemoveTrigger(actionConfig);
						removeTrigger.Init();
						temp = removeTrigger;
						break;
					case "TriggerAction_ExcuteTrigger":
						var excuteTrigger = new TriggerAction_ExcuteTrigger(actionConfig);
						excuteTrigger.Init();
						temp = excuteTrigger;
						break;
					case "TriggerAction_AddBuff":
						var addBuff = new TriggerAction_AddBuff(actionConfig);
						addBuff.Init();
						temp = addBuff;
						break;
					case "TriggerAction_RemoveBuff":
						var removeBuff = new TriggerAction_RemoveBuff(actionConfig);
						removeBuff.Init();
						temp = removeBuff;
						break;
					case "TriggerAction_Action":
						var action = new TriggerAction_Action(actionConfig);
						action.Init(i < actionList1.Count ? _action1 : _action2);
						temp = action;
						break;
				}
				if (i < actionList1.Count) actionList1.Add(temp);
				else actionList2.Add(temp);
			}
		}
		public virtual void Excute(params object[] param)
        {

        }
	}
}
