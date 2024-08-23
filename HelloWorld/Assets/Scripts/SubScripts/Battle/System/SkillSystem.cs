namespace HotAssembly
{
    public interface SkillSystemInterface : ECS_SystemInterface
    {
        void UpdateSkill(float t);
    }
    public class SkillSystem : ECS_System<SkillSystemInterface>
    {
        protected override void Update(SkillSystemInterface entity, float t)
        {
            entity.UpdateSkill(t);
        }
    }
}