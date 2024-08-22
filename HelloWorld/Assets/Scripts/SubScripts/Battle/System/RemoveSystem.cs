namespace HotAssembly
{
    public class RemoveSystem : ECS_System<ECS_Entity>
    {
        public override void Update(float t)
        {
            base.Update(t);
            Clear();
        }
        protected override void Update(ECS_Entity entity, float t)
        {
            entity.Clear();
        }
    }
}