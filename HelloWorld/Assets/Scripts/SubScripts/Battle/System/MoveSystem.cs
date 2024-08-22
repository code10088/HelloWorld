namespace HotAssembly
{
    public interface MoveSystemInterface : ECS_SystemInterface
    {
        MoveComponent Move { get; }
    }
    public class MoveSystem : ECS_System<MoveSystemInterface>
    {
        protected override void Update(MoveSystemInterface entity, float t)
        {
            entity.Move.Update(t);
        }
    }
}