using System;
using System.Collections.Generic;

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
		for (int i = 0; i < buffs.Count; i++)
		{
			if (buffs[i].BuffID == buffId)
			{
				buffs[i].Remove();
				return;
			}
		}
	}
	public void RemoveBuffByConfig(int configId)
	{
		for (int i = 0; i < buffs.Count; i++)
		{
			if (buffs[i].ConfigId == configId)
			{
				buffs[i].Remove();
			}
		}
	}
	public void Update(float t)
	{
		for (int i = 0; i < buffs.Count; i++)
		{
			var temp = buffs[i];
			temp.Excute(t);
			if (temp.EndMark)
			{
				buffs.RemoveAt(i);
				i--;
			}
		}
	}
	public void Clear()
	{
		buffs.Clear();
	}
}