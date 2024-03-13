using cfg;
using System;
using System.Collections.Generic;

namespace HotAssembly
{
	public class TriggerManager
	{
		private Dictionary<TriggerMode, List<TriggerBase>> triggers = new Dictionary<TriggerMode, List<TriggerBase>>();

		public void AddTrigger(int configId, Func<bool> condition, Action action1, Action action2)
        {
			var config = ConfigManager.Instance.GameConfigs.TbTrigger[configId];
			if (triggers.TryGetValue(config.TriggerMode, out List<TriggerBase> list))
            {
				if (config.Limit > 0)
				{
					int count = 0;
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].ConfigId == configId) count++;
						if (count >= config.Limit) return;
					}
				}
			}
            else
            {
				list = new List<TriggerBase>();
				triggers.Add(config.TriggerMode, list);
			}

			TriggerBase temp = GetTriggerObject(config.TriggerType);
			temp.Init(config, condition, action1, action2);

			bool mark = true;
            for (int i = 0; i < list.Count; i++)
            {
				if (list[i].Priority <= temp.Priority)
				{
					list.Insert(i, temp);
					mark = false;
					break;
				}
            }
			if (mark) list.Add(temp);
		}
		private TriggerBase GetTriggerObject(string type)
        {
            //Type t = Type.GetType("HotAssembly." + type);
            //return Activator.CreateInstance(t) as TriggerBase;
            switch (type)
            {
				case "Trigger_Normal": return new Trigger_Normal();
				default: return new Trigger_Normal();
			}
		}
		public void RemoveTrigger(int triggerId)
        {
            foreach (var item in triggers)
            {
				List<TriggerBase> list = item.Value;
				for (int i = list.Count - 1; i >= 0; i--)
                {
					if(list[i].TriggerID == triggerId)
                    {
						list.RemoveAt(i);
						return;
                    }
				}
            }
        }
		public void RemoveTriggerByConfig(int configId)
        {
			foreach (var item in triggers)
			{
				List<TriggerBase> list = item.Value;
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (list[i].ConfigId == configId)
					{
						list.RemoveAt(i);
					}
				}
			}
		}
		public void ExcuteTrigger(TriggerMode triggerMode, params object[] param)
        {
			if (triggers.TryGetValue(triggerMode, out List<TriggerBase> list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					list[i].Excute(param);
				}
			}
		}
	}
}
