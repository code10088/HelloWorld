public class StateComponent : ECS_Component
{
    private MonsterEntity entity;

    public void Init(MonsterEntity entity)
    {
        this.entity = entity;
    }
    public void Update(float t)
    {
        entity.UpdateState();
    }
    public void Clear()
    {
        entity = null;
    }
}