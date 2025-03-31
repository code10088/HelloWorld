public interface ECS_SystemInterface
{
    void Init();
}
public class ECS_System<T> where T : class, ECS_SystemInterface
{
    protected ArrayEx<T> entities = new ArrayEx<T>(100);

    public void AddEntity(T entity)
    {
        entities.Add(entity);
    }
    public void RemoveEntity(T entity)
    {
        entities.Remove(entity);
    }
    public void Clear()
    {
        entities.Clear();
    }
    public virtual void Update(float t)
    {
        T e;
        for (int i = 0; i < entities.Count; i++)
        {
            e = entities[i];
            if (e != null) Update(e, t);
        }
    }
    protected virtual void Update(T entity, float t)
    {

    }
}