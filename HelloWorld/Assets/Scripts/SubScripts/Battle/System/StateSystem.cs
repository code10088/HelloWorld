public interface StateSystemInterface : ECS_SystemInterface
{
    void UpdateState(float t);
}
public class StateSystem : ECS_System<StateSystemInterface>
{
    protected override void Update(StateSystemInterface entity, float t)
    {
        entity.UpdateState(t);
    }
}