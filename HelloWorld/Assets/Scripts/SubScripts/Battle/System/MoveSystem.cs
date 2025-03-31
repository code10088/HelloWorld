public interface MoveSystemInterface : ECS_SystemInterface
{
    void UpdateMove(float t);
}
public class MoveSystem : ECS_System<MoveSystemInterface>
{
    protected override void Update(MoveSystemInterface entity, float t)
    {
        entity.UpdateMove(t);
    }
}