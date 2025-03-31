using cfg;

public class ActivityDataBase
{
    private int activityId;
    private long startTime;
    private long endTime;
    private ActivityConfig config;

    public int ActivityId => activityId;
    public long StartTime => startTime;
    public long EndTime => endTime;
    public ActivityConfig Config => config;

    public void Init(int activityId, ActivityConfig config)
    {
        this.activityId = activityId;
        this.config = config;
        //请求服务器数据

    }
    public void Clear()
    {
        config = null;
    }

    public void Refresh(long startTime, long endTime)
    {
        this.startTime = startTime;
        this.endTime = endTime;
    }
    public void Refresh()
    {
        EventManager.Instance.FireEvent(EventType.RefreshActivity, activityId);
    }
    public virtual void End()
    {
        EventManager.Instance.FireEvent(EventType.EndActivity, activityId);
    }
    public virtual int CheckRedPoint()
    {
        return 0;
    }
}