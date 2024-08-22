namespace HotAssembly
{
    public interface SkillSystemInterface : ECS_SystemInterface
    {
        SkillComponent Skill { get; }
    }
    public class SkillSystem : ECS_System<SkillSystemInterface>
    {
        protected override void Update(SkillSystemInterface entity, float t)
        {
            entity.Skill.Update(t);
        }
    }
}