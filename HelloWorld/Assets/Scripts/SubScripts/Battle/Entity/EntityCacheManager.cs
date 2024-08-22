namespace HotAssembly
{
    public class EntityCacheManager : Singletion<EntityCacheManager>
    {
        public DamageNumCache DamageNumCache = new DamageNumCache();
        public FightCache FightCache = new FightCache();
        public SkillCache SkillCache = new SkillCache();
        public void Clear()
        {
            DamageNumCache.Clear();
            FightCache.Clear();
            SkillCache.Clear();
        }
    }
}