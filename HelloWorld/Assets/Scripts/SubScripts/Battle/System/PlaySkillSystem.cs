namespace HotAssembly
{
    public interface PlaySkillSystemInterface : ECS_SystemInterface
    {
        PlaySkillComponent Play { get; }
    }
    public class PlaySkillSystem : ECS_System<PlaySkillSystemInterface>
    {
        protected override void Update(PlaySkillSystemInterface entity, float t)
        {
            entity.Play.Update(t);
        }
    }
}