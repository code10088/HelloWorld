using cfg;

namespace HotAssembly
{
    public class SkillComponent : ECS_Component
    {
        private SkillEntity entity;
        private SkillConfig config;
        private float timer = 0;

        public SkillConfig Config => config;

        public void Init(SkillEntity entity, SkillConfig config)
        {
            this.entity = entity;
            this.config = config;
        }
        public void Update(float t)
        {
            timer += t;
            if (config.Duration > 0 && timer >= config.Duration)
            {
                entity.Remove();
                return;
            }
            if (timer <= config.Delay)
            {
                return;
            }
            if (config.Duration == 0)
            {
                entity.Remove();
            }
            entity.PlaySkill(timer);
        }
        public void Clear()
        {
            entity = null;
            config = null;
            timer = 0;
        }
    }
}