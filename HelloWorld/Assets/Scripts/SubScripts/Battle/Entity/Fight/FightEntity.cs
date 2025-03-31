public class FightEntity : ECS_Entity
{
    protected int allyId;
    protected int teamId;
    protected FightEntity target;
    protected GameObjectComponent obj;
    protected TransformComponent transform;
    protected AttrComponent attr;
    protected AniComponent ani;

    public bool Active => itemId > 0;
    public int AllyId => allyId;
    public FightEntity Target => target;
    public TransformComponent Transform => transform;
    public AttrComponent Attr => attr;
    public AniComponent Ani => ani;

    public void Init(int allyId)
    {
        this.allyId = allyId;
    }
}