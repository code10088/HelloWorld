namespace HotAssembly
{
    public class SystemManager : Singletion<SystemManager>
    {
        public RemoveSystem RemoveSystem = new RemoveSystem();
        public MoveSystem MoveSystem = new MoveSystem();
        public PlaySkillSystem PlaySkillSystem = new PlaySkillSystem();
        public StateSystem StateSystem = new StateSystem();
        public SkillSystem SkillSystem = new SkillSystem();

        public void Update(float t)
        {
            RemoveSystem.Update(t);
            MoveSystem.Update(t);
            PlaySkillSystem.Update(t);
            StateSystem.Update(t);
            SkillSystem.Update(t);
        }
        public void Clear()
        {
            RemoveSystem.Clear();
            MoveSystem.Clear();
            PlaySkillSystem.Clear();
            StateSystem.Clear();
            SkillSystem.Clear();
        }
    }
}