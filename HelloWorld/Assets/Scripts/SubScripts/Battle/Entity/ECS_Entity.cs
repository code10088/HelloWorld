public class ECS_Entity : ECS_SystemInterface
{
    private static int uniqueId = 0;
    protected int itemId;

    public int ItemId => itemId;

    public virtual void Init()
    {
        itemId = ++uniqueId;
    }
    public virtual void Clear()
    {
        itemId = -1;
    }
    public void Remove()
    {
        SystemManager.Instance.RemoveSystem.AddEntity(this);
    }
}