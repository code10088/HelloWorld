using System.Collections.Generic;

public class ActivityData : DataBase
{
    private List<ActivityDataBase> datas = new List<ActivityDataBase>();
    private int timerId = -1;

    public void Clear()
    {
        datas.Clear();
        TimeManager.Instance.StopTimer(timerId);
    }

    public void Init(List<TempActivityItem> data)
    {
        datas.Clear();
        for (int i = 0; i < data.Count; i++) Refresh(data[i]);
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(0, 1, CheckActivity);
    }
    public void Refresh(TempActivityItem data)
    {
        var temp = datas.Find(a => a.ActivityId == data.activityId);
        if (temp == null)
        {
            var config = ConfigManager.Instance.GameConfigs.TbActivityConfig[data.configId];
            switch (config.ActivityType)
            {
                case cfg.ActivityType.Turntable:
                    temp = new ActivityData_Turntable();
                    break;
            }
            temp.Init(data.activityId, config);
            datas.Add(temp);
        }
        else
        {
            temp.Refresh(data.startTime, data.endTime);
            if (TimeUtils.ServerTime > temp.EndTime)
            {
                temp.End();
                datas.Remove(temp);
            }
        }
    }
    private void CheckActivity(float f)
    {
        for (int i = datas.Count - 1; i >= 0; i--)
        {
            if (TimeUtils.ServerTime > datas[i].EndTime)
            {
                datas[i].End();
                datas.RemoveAt(i);
            }
        }
    }
    public T GetActivityData<T>(int id) where T : ActivityDataBase
    {
        var temp = datas.Find(a => a.ActivityId == id);
        return temp as T;
    }
}

public class TempActivityItem
{
    public int activityId;
    public long startTime;
    public long endTime;
    public int configId;
}