public class EntityCache
{
    protected ArrayEx<ECS_Entity> entities = new ArrayEx<ECS_Entity>(1000);
    protected ArrayEx<ECS_Entity> cache = new ArrayEx<ECS_Entity>(1000);

    public T GetEntity<T>(int id) where T : ECS_Entity
    {
        var result = entities.Find(a => a?.ItemId == id);
        if (result == null) return null;
        else return result as T;
    }
    public void Remove(ECS_Entity entity)
    {
        cache.Add(entity);
        entities.Remove(entity);
    }
    public void Clear()
    {
        for (int i = 0; i < entities.Count; i++) entities[i]?.Clear();
        entities.Clear();
        cache.Clear();
    }
}