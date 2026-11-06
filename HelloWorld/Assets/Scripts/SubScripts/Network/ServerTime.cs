public class ServerTime:Singleton<ServerTime>, SingletonInterface
{
    private long time;

    public long Time => time;

    public void Init()
    {

    }
    public void SetTime(long time)
    {
        this.time = time;
    }
}
