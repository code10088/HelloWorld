namespace HotAssembly
{
    public class FightEntity : ECS_Entity
    {
        protected int allyId;
        protected int teamId;
        protected FightEntity target;

        public bool Active => itemId > 0;
        public int AllyId => allyId;
        public FightEntity Target => target;

        protected GameObjectComponent obj;
        protected AttrComponent attr;
        public AttrComponent Attr => attr;
        protected TransformComponent transform;
        public TransformComponent Transform => transform;
        protected AniComponent ani;
        public AniComponent Ani => ani;

        public void Init(int allyId)
        {
            this.allyId = allyId;
        }
    }
}