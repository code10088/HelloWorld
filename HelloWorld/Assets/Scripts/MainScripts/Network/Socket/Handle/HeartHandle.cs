using ProtoBuf;
using System;
using System.Threading.Tasks;

public class HeartHandle
{
    [ProtoContract]
    public class CS_Heart : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
    }


    private int heartTimerId = 0;
    private int heartTimer = 0;
    private int heartInterval = 10;
    private int heartCount = 0;
    private int[] record1 = new int[10];
    private long[] record2 = new long[10];
    private int recordIndex = 0;
    private int delay = 0;
    private Func<Task<bool>> connect;
    private Action<ushort, IExtensible> send;
    private CS_Heart heart = new CS_Heart();

    public int Delay => delay;

    public HeartHandle(Func<Task<bool>> connect, Action<ushort, IExtensible> send)
    {
        this.connect = connect;
        this.send = send;
    }
    public void Start()
    {
        heartTimerId = Driver.Instance.StartTimer(0, 1, UpdateHeart);
    }
    public void UpdateHeart(float t)
    {
        heartTimer++;
        if (heartTimer < heartInterval) return;
        heartTimer = 0;
        if (heartCount++ > 0) connect();
        else send(0, heart);
    }
    public void RefreshDelay1(ushort id)
    {
        int index = recordIndex++ % record1.Length;
        record1[index] = id;
        record2[index] = DateTime.UtcNow.Ticks;
    }
    public void RefreshDelay2(ushort id)
    {
        heartTimer = 0;
        id -= 10000;
        if (id == 0) heartCount = 0;
        var index = Array.IndexOf(record1, id);
        if (index < 0) return;
        record1[index] = -1;
        delay = (int)((DateTime.UtcNow.Ticks - record2[index]) / 10000);
        heartInterval = delay > 100 ? 3 : 10;
    }
    public void Dispose()
    {
        Driver.Instance.Remove(heartTimerId);
        heartTimer = 0;
        heartCount = 0;
        recordIndex = 0;
    }
}
