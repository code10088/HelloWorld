using System;
using System.Collections.Generic;

namespace HotAssembly
{
	public class BuffManager
	{
		private List<BuffItem> buffs = new List<BuffItem>();

		public void AddBuff(int configId, Func<bool> condition = null, Action action = null)
        {
			var config = ConfigManager.Instance.GameConfigs.TbBuff[configId];
			var list = buffs.FindAll(a => a.ConfigId == configId);
			if (config.Limit > 0 && list.Count >= config.Limit) return;

			BuffItem temp = new BuffItem();
			temp.Init(this, config, condition, action);

			bool mark = true;
            for (int i = 0; i < buffs.Count; i++)
            {
				if (buffs[i].Priority <= temp.Priority)
				{
					buffs.Insert(i, temp);
					mark = false;
					break;
				}
            }
			if (mark) buffs.Add(temp);
		}
		public void RemoveBuff(int buffId)
        {
			int index = buffs.FindIndex(a => a.BuffID == buffId);
			if (index >= 0) buffs.RemoveAt(index);
		}
		public void RemoveBuffByConfig(int configId)
        {
			buffs.RemoveAll(a => a.ConfigId == configId);
		}
		public void Update()
        {
			List<int> remove = new List<int>();
			for (int i = 0; i < buffs.Count; i++)
			{
				bool b = buffs[i].Excute();
				if (b) remove.Add(buffs[i].BuffID);
			}
			for (int i = 0; i < remove.Count; i++)
			{
				RemoveBuff(remove[i]);
			}
		}
	}
}
