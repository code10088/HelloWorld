using cfg;
using System;
using System.Collections.Generic;

namespace HotAssembly
{
	public class TriggerManager
	{
		private Dictionary<TriggerMode, List<TriggerItem>> triggers = new Dictionary<TriggerMode, List<TriggerItem>>();

		public void AddTrigger(int configId, Func<bool> condition = null, Action action1 = null, Action action2 = null)
        {
			var config = ConfigManager.Instance.GameConfigs.TbTrigger[configId];
			if (triggers.TryGetValue(config.TriggerMode, out List<TriggerItem> list))
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
				list = new List<TriggerItem>();
				triggers.Add(config.TriggerMode, list);
			}

			TriggerItem temp = new TriggerItem();
			temp.Init(this, config, condition, action1, action2);

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
		public void RemoveTrigger(int triggerId)
        {
            foreach (var item in triggers)
            {
				List<TriggerItem> list = item.Value;
                for (int i = 0; i < list.Count; i++)
                {
					if (list[i].TriggerID == triggerId)
					{
						list[i].Remove();
						return;
					}
				}
            }
        }
		public void RemoveTriggerByConfig(int configId)
        {
			foreach (var item in triggers)
			{
				List<TriggerItem> list = item.Value;
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].ConfigId == configId)
					{
						list[i].Remove();
					}
				}
			}
		}
		public void ExcuteTrigger(TriggerMode triggerMode, params object[] param)
        {
			if (triggers.TryGetValue(triggerMode, out List<TriggerItem> list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					var temp = list[i];
					temp.Excute(param);
					if (temp.EndMark)
					{
						list.RemoveAt(i);
						i--;
					}
				}
			}
		}
		public void Update(float t)
		{
			foreach (var item in triggers)
			{
				for (int i = 0; i < item.Value.Count; i++)
				{
					item.Value[i].Update(t);
				}
			}
		}
	}
}
