namespace HotAssembly
{
    public interface StateSystemInterface : ECS_SystemInterface
    {
        StateComponent State { get; }
    }
    public class StateSystem : ECS_System<StateSystemInterface>
    {
        protected override void Update(StateSystemInterface entity, float t)
        {
            entity.State.Update(t);
        }
    }
}