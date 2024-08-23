namespace HotAssembly
{
    public interface PlaySkillSystemInterface : ECS_SystemInterface
    {
        void UpdatePlaySkill(float t);
    }
    public class PlaySkillSystem : ECS_System<PlaySkillSystemInterface>
    {
        protected override void Update(PlaySkillSystemInterface entity, float t)
        {
            entity.UpdatePlaySkill(t);
        }
    }
}